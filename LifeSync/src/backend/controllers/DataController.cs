using Microsoft.AspNetCore.Mvc;
using backend.models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LifeSync.Controllers
{
    [Route("api")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly LifeSyncDbContext _context;

        public DataController(LifeSyncDbContext context)
        {
            _context = context;
        }

        [HttpGet("fetch-notion-data")]
        public async Task<IActionResult> FetchNotionData()
        {
            try
            {
                var accessToken = await _context.OAuthTokens
                    .Where(t => t.Source.ToLower() == "notion")
                    .OrderByDescending(t => t.ExpiryDate)
                    .Select(t => t.AccessToken)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(accessToken))
                    return NotFound(new { error = "Notion token bulunamadı." });

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

                var databaseId = "1c0b360d762580278f1cc03200fed541";
                var notionResponse = await client.PostAsync($"https://api.notion.com/v1/databases/{databaseId}/query", null);

                if (!notionResponse.IsSuccessStatusCode)
                {
                    var error = await notionResponse.Content.ReadAsStringAsync();
                    return StatusCode((int)notionResponse.StatusCode, new { error = "Notion API hatası", detail = error });
                }

                var rawData = await notionResponse.Content.ReadAsStringAsync();
                return Ok(new { data = rawData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Sunucu hatası", detail = ex.Message });
            }
        }

        [HttpGet("notion/fetch")]
        public async Task<IActionResult> FetchNotionDataDirect()
        {
            return await FetchNotionData();
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] SyncDataRequest request)
        {
            try
            {
                if (request?.Data == null || !request.Data.Any())
                    return BadRequest("Geçersiz veri: Boş liste.");

                var source = request.Source?.ToLowerInvariant() ?? "";
                Guid userId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48");

                if (source == "todoist" || source == "notion")
                {
                    var tokenUserId = await _context.OAuthTokens
                        .Where(t => t.Source.ToLower() == source)
                        .OrderByDescending(t => t.ExpiryDate)
                        .Select(t => t.UserId)
                        .FirstOrDefaultAsync();

                    if (tokenUserId != Guid.Empty)
                        userId = tokenUserId;
                }

                if (source == "todoist")
                    _context.Tasks.RemoveRange(_context.Tasks.Where(t => t.Source == source && t.UserId == userId));
                else if (source == "notion")
                    _context.Notes.RemoveRange(_context.Notes.Where(n => n.Source == source && n.UserId == userId));

                foreach (var item in request.Data)
                {
                    Guid itemId = Guid.TryParse(item.Id, out var parsed) ? parsed : Guid.NewGuid();

                    if (source == "todoist" || source == "lifesync-task")
                    {
                        DateTime? parsedDueDate = null;
                        if (!string.IsNullOrWhiteSpace(item.DueDate) && DateTime.TryParse(item.DueDate, out var due))
                        {
                            if (due.Year >= 1900)
                                parsedDueDate = DateTime.SpecifyKind(due, DateTimeKind.Local).ToUniversalTime();
                        }

                        var createdAt = item.CreatedAt.Kind == DateTimeKind.Utc
                            ? item.CreatedAt
                            : DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Local).ToUniversalTime();

                        _context.Tasks.Add(new TaskItem
                        {
                            Id = itemId,
                            Content = item.Content,
                            DueDate = parsedDueDate,
                            Completed = item.Completed,
                            CreatedAt = createdAt,
                            Source = source,
                            UserId = userId
                        });
                    }
                    else if (source == "notion" || source == "lifesync")
                    {
                        _context.Notes.Add(new Note
                        {
                            Id = itemId,
                            Content = item.Content,
                            CreatedAt = item.CreatedAt.ToUniversalTime(),
                            Source = source,
                            UserId = userId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"{request.Source} verileri güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Sync hatası: {ex.Message}" });
            }
        }

        [HttpPut("todoist/complete/{id}")]
        public async Task<IActionResult> MarkTaskCompleted(Guid id)
        {
            try
            {
                var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
                if (task == null)
                    return NotFound();

                task.Completed = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Görev veritabanında tamamlandı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("todoist/close/{id}")]
        public async Task<IActionResult> CloseTodoistTask(string id)
        {
            try
            {
                var token = await _context.OAuthTokens
                    .Where(t => t.Source.ToLower() == "todoist")
                    .OrderByDescending(t => t.ExpiryDate)
                    .Select(t => t.AccessToken)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(token))
                    return BadRequest(new { error = "Todoist token bulunamadı." });

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsync($"https://api.todoist.com/rest/v2/tasks/{id}/close", null);

                if (!response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { error = "Todoist API hatası", detail });
                }

                return Ok(new { message = "Todoist'te görev başarıyla tamamlandı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{source}/data")]
        public async Task<IActionResult> GetData(string source)
        {
            try
            {
                switch (source.ToLower())
                {
                    case "todoist":
                    case "lifesync-task":
                        return Ok(await _context.Tasks
                            .Where(t => t.Source.ToLower() == source.ToLower())
                            .OrderByDescending(t => t.CreatedAt)
                            .ToListAsync());
                    case "notion":
                    case "lifesync":
                        return Ok(await _context.Notes
                            .Where(n => n.Source.ToLower() == source.ToLower())
                            .OrderByDescending(n => n.CreatedAt)
                            .ToListAsync());
                    default:
                        return NotFound(new { error = "Veri çekme hatası: Geçersiz kaynak" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Veri çekme hatası: {ex.Message}" });
            }
        }

        [HttpGet("get-token")]
        public async Task<IActionResult> GetToken(string source)
        {
            try
            {
                var token = await _context.OAuthTokens
                    .Where(t => t.Source.ToLower() == source.ToLower())
                    .OrderByDescending(t => t.ExpiryDate)
                    .Select(t => t.AccessToken)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(token))
                    return BadRequest(new { error = "Token bulunamadı" });

                return Ok(new { accessToken = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Token getirme hatası: {ex.Message}" });
            }
        }

        [HttpPost("ai-suggest")]
        public async Task<IActionResult> GetAISuggestion([FromBody] AIRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Task + request.Description + request.Category))
                    return BadRequest("Gönderilen veri boş veya eksik.");

                var payload = new
                {
                    task = request.Task,
                    description = request.Description,
                    category = request.Category
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                var aiResponse = await client.PostAsync("http://127.0.0.1:5001/suggest", content);

                if (!aiResponse.IsSuccessStatusCode)
                {
                    var errorDetail = await aiResponse.Content.ReadAsStringAsync();
                    return StatusCode((int)aiResponse.StatusCode, new { error = "AI sunucu hatası", detail = errorDetail });
                }

                var result = await aiResponse.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("ai/save-task")]
        public async Task<IActionResult> SaveSuggestedTask([FromBody] AITaskRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest("Görev adı boş olamaz.");

                var task = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Content = $"{request.Name} | {request.Category} | Tahmini Süre: {request.EstimatedTime}",
                    CreatedAt = DateTime.UtcNow,
                    Completed = false,
                    Source = "lifesync-task",
                    UserId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48")
                };

                await _context.Tasks.AddAsync(task);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Görev kaydedildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class SyncDataRequest
    {
        public string Source { get; set; } = string.Empty;
        public List<SyncDataItem> Data { get; set; } = new();
    }

    public class SyncDataItem
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? DueDate { get; set; }
        public string? StartDate { get; set; }
        public bool Completed { get; set; }
    }

    public class AIRequest
    {
        public string Task { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
    }

    public class AITaskRequest
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string EstimatedTime { get; set; } = "";
    }
}
