using Microsoft.AspNetCore.Mvc;
using backend.models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] SyncDataRequest request)
        {
            try
            {
                if (request?.Data == null || !request.Data.Any())
                    return BadRequest("Geçersiz veri: Boş liste.");

                var source = request.Source?.ToLowerInvariant() ?? "";
                Guid userId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48");

                //LifeSync özel kontrolü burada
                if (source == "lifesync")
                {
                    userId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48");
                }
                else if (source == "todoist" || source == "notion")
                {
                    var tokenUserId = await _context.OAuthTokens
                        .Where(t => t.Source.ToLower() == source)
                        .OrderByDescending(t => t.ExpiryDate)
                        .Select(t => t.UserId)
                        .FirstOrDefaultAsync();

                    if (tokenUserId != Guid.Empty)
                        userId = tokenUserId;
                }

                //Eski verileri sil
                if (source == "todoist")
                {
                    var eski = _context.Tasks.Where(t => t.Source == source && t.UserId == userId);
                    _context.Tasks.RemoveRange(eski);
                }
                else if (source == "notion")
                {
                    var eski = _context.Notes.Where(n => n.Source == source && n.UserId == userId);
                    _context.Notes.RemoveRange(eski);
                }

                //Yeni verileri ekle
                foreach (var item in request.Data)
                {
                    if (!Guid.TryParse(item.Id, out Guid itemId))
                        itemId = Guid.NewGuid();

                    if (source == "todoist")
                    {
                        _context.Tasks.Add(new TaskItem
                        {
                            Id = itemId,
                            Content = item.Content,
                            DueDate = string.IsNullOrEmpty(item.DueDate) ? null : DateTime.Parse(item.DueDate),
                            Completed = item.Completed,
                            CreatedAt = item.CreatedAt.Kind == DateTimeKind.Utc
                                        ? item.CreatedAt
                                        : item.CreatedAt.ToUniversalTime(),
                            Source = "todoist",
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
                return StatusCode(500, $"Sync hatası: {ex.Message}");
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
                        return Ok(await _context.Tasks.Where(t => t.Source == source).ToListAsync());

                    case "notion":
                    case "lifesync":
                        return Ok(await _context.Notes
                            .Where(n => n.Source == source)
                            .OrderByDescending(n => n.CreatedAt)
                            .ToListAsync());

                    default:
                        return NotFound("Geçersiz kaynak");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Veri çekme hatası: {ex.Message}");
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
}
