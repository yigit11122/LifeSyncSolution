using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using backend.models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;

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

        [HttpGet("{source}/data")]
        public async Task<IActionResult> GetData(string source)
        {
            try
            {
                switch (source.ToLower())
                {
                    case "todoist":
                        return Ok(await _context.Tasks.Where(t => t.Source == source).ToListAsync());
                    case "googlecalendar":
                        return Ok(await _context.Events.Where(e => e.Source == source).ToListAsync());
                    case "notion":
                        return Ok(await _context.Notes.Where(n => n.Source == source).ToListAsync());
                    case "fitbit":
                    case "lifesync":
                        return Ok(await _context.Tasks.Where(t => t.Source == source).ToListAsync());
                    default:
                        return NotFound("Geçersiz kaynak");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Veri çekme hatası: {ex.Message}");
            }
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] SyncDataRequest request)
        {
            try
            {
                if (request?.Data == null || !request.Data.Any())
                    return BadRequest("Geçersiz veri: Veri listesi boş veya null.");

                Console.WriteLine($"Sync isteği alındı: Source = {request.Source}, Veri sayısı = {request.Data.Count}");

                foreach (var item in request.Data)
                {
                    Console.WriteLine($"Veri işleniyor: Id = {item.Id}, Content = {item.Content}, CreatedAt = {item.CreatedAt}");

                    Guid userId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48"); // default

                    if (request.Source.ToLower() == "notion")
                    {
                        var tokenUserId = await _context.OAuthTokens
                            .Where(t => t.Source.ToLower() == "notion")
                            .OrderByDescending(t => t.ExpiryDate)
                            .Select(t => t.UserId)
                            .FirstOrDefaultAsync();

                        if (tokenUserId != Guid.Empty)
                            userId = tokenUserId;
                    }

                    switch (request.Source.ToLower())
                    {
                        case "todoist":
                            _context.Tasks.Add(new TaskItem
                            {
                                Id = Guid.Parse(item.Id),
                                Content = item.Content,
                                DueDate = string.IsNullOrEmpty(item.DueDate) ? null : DateTime.Parse(item.DueDate),
                                Completed = item.Completed,
                                Source = "todoist",
                                UserId = userId
                            });
                            break;

                        case "googlecalendar":
                            _context.Events.Add(new Event
                            {
                                Id = Guid.Parse(item.Id),
                                Summary = item.Content,
                                StartDate = string.IsNullOrEmpty(item.StartDate) ? DateTime.UtcNow : DateTime.Parse(item.StartDate),
                                Source = "googleCalendar",
                                UserId = userId
                            });
                            break;

                        case "notion":
                            var noteId = Guid.Parse(item.Id);

                            // 🔄 Varsa eski kaydı sil
                            var existing = await _context.Notes.FindAsync(noteId);
                            if (existing != null)
                            {
                                _context.Notes.Remove(existing);
                                await _context.SaveChangesAsync();
                            }

                            _context.Notes.Add(new Note
                            {
                                Id = noteId,
                                Content = item.Content,
                                CreatedAt = DateTime.Parse(item.CreatedAt).ToUniversalTime(),
                                Source = "notion",
                                UserId = userId
                            });
                            break;

                        case "fitbit":
                        case "lifesync":
                            _context.Tasks.Add(new TaskItem
                            {
                                Id = Guid.Parse(item.Id),
                                Content = item.Content,
                                DueDate = string.IsNullOrEmpty(item.CreatedAt) ? null : DateTime.Parse(item.CreatedAt).ToUniversalTime(),
                                Source = request.Source,
                                UserId = userId
                            });
                            break;

                        default:
                            return BadRequest("Geçersiz kaynak: " + request.Source);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Veri başarıyla kaydedildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Veri kaydetme hatası: {ex.Message}");
            }
        }

        [HttpGet("get-notes")]
        public async Task<IActionResult> GetNotes(string source)
        {
            try
            {
                var notes = await _context.Notes
                    .Where(n => n.Source.ToLower() == source.ToLower())
                    .Select(n => new { n.Id, n.Content, n.CreatedAt, n.Source })
                    .ToListAsync();

                return Ok(notes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Notlar çekilirken hata oluştu: {ex.Message}" });
            }
        }

        [HttpGet("fetch-notion-data")]
        public async Task<IActionResult> FetchNotionData()
        {
            try
            {
                Console.WriteLine("Notion veri çekme isteği alındı.");
                string accessToken;

                using (var scope = HttpContext.RequestServices.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<LifeSyncDbContext>();
                    accessToken = await context.OAuthTokens
                        .Where(t => t.Source.ToLower() == "notion")
                        .OrderByDescending(t => t.ExpiryDate)
                        .Select(t => t.AccessToken)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrEmpty(accessToken))
                        return new JsonResult(new { error = "Token alınmadı" });
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

                string databaseId = "1c0b360d762580278f1cc03200fed541";
                var notionResponse = await client.PostAsync($"https://api.notion.com/v1/databases/{databaseId}/query", null);

                if (!notionResponse.IsSuccessStatusCode)
                {
                    var error = await notionResponse.Content.ReadAsStringAsync();
                    return new JsonResult(new { error = $"Notion veri çekme hatası: {error}" });
                }

                var notionData = await notionResponse.Content.ReadAsStringAsync();
                return new JsonResult(new { data = notionData });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Veri çekme hatası: {ex.Message}" });
            }
        }
    }

    public class SyncDataRequest
    {
        public string Source { get; set; }
        public List<SyncDataItem> Data { get; set; }
    }

    public class SyncDataItem
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public string? DueDate { get; set; }
        public string? StartDate { get; set; }
        public string CreatedAt { get; set; }
        public bool Completed { get; set; }
    }
}