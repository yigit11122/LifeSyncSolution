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
                        var todoistData = await _context.Tasks.Where(t => t.Source == source).ToListAsync();
                        return Ok(todoistData);
                    case "googlecalendar":
                        var googleData = await _context.Events.Where(e => e.Source == source).ToListAsync();
                        return Ok(googleData);
                    case "notion":
                        var notionData = await _context.Notes.Where(n => n.Source == source).ToListAsync();
                        return Ok(notionData);
                    case "fitbit":
                        var fitbitData = await _context.Tasks.Where(t => t.Source == source).ToListAsync();
                        return Ok(fitbitData);
                    case "lifesync":
                        var lifesyncData = await _context.Tasks.Where(t => t.Source == source).ToListAsync();
                        return Ok(lifesyncData);
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
                if (request == null || request.Data == null || !request.Data.Any())
                {
                    Console.WriteLine("Hata: Geçersiz veri: Veri listesi boş veya null.");
                    return BadRequest("Geçersiz veri: Veri listesi boş veya null.");
                }

                Console.WriteLine($"Sync isteği alındı: Source = {request.Source}, Veri sayısı = {request.Data.Count}");
                foreach (var item in request.Data)
                {
                    Console.WriteLine($"Veri işleniyor: Id = {item.Id}, Content = {item.Content}, CreatedAt = {item.CreatedAt}");
                    switch (request.Source.ToLower())
                    {
                        case "todoist":
                            _context.Tasks.Add(new TaskItem
                            {
                                Id = Guid.Parse(item.Id),
                                Content = item.Content,
                                DueDate = item.DueDate != null ? DateTime.Parse(item.DueDate) : null,
                                Completed = item.Completed,
                                Source = "todoist",
                                UserId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48")
                            });
                            break;
                        case "googlecalendar":
                            _context.Events.Add(new Event
                            {
                                Id = Guid.Parse(item.Id),
                                Summary = item.Content,
                                StartDate = item.StartDate != null ? DateTime.Parse(item.StartDate) : DateTime.UtcNow,
                                Source = "googleCalendar",
                                UserId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48")
                            });
                            break;
                        case "notion":
                            // CreatedAt değerini UTC formatına çeviriyoruz
                            var createdAt = DateTime.Parse(item.CreatedAt).ToUniversalTime();
                            _context.Notes.Add(new Note
                            {
                                Id = Guid.Parse(item.Id),
                                Content = item.Content,
                                CreatedAt = createdAt,
                                Source = "notion",
                                UserId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48")
                            });
                            break;
                        case "fitbit":
                        case "lifesync":
                            _context.Tasks.Add(new TaskItem
                            {
                                Id = Guid.Parse(item.Id),
                                Content = item.Content,
                                DueDate = item.CreatedAt != null ? DateTime.Parse(item.CreatedAt).ToUniversalTime() : null,
                                Source = request.Source,
                                UserId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48")
                            });
                            break;
                        default:
                            Console.WriteLine($"Hata: Geçersiz kaynak: {request.Source}");
                            return BadRequest("Geçersiz kaynak: " + request.Source);
                    }
                }
                await _context.SaveChangesAsync();
                Console.WriteLine("Veri başarıyla kaydedildi.");
                return Ok(new { message = "Veri başarıyla kaydedildi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Veri kaydetme hatası: {ex.Message}");
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
                    .Select(n => new
                    {
                        id = n.Id,
                        content = n.Content,
                        createdAt = n.CreatedAt,
                        source = n.Source
                    })
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
                    {
                        Console.WriteLine("Hata: Token alınmadı.");
                        return new JsonResult(new { error = "Token alınmadı" });
                    }
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

                string databaseId = "1c0b360d762580278f1cc03200fed541";
                Console.WriteLine($"Notion API isteği gönderiliyor: DatabaseId = {databaseId}");
                var notionResponse = await client.PostAsync($"https://api.notion.com/v1/databases/{databaseId}/query", null);

                if (!notionResponse.IsSuccessStatusCode)
                {
                    var error = await notionResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Notion veri çekme hatası: {error}");
                    return new JsonResult(new { error = $"Notion veri çekme hatası: {error}" });
                }

                var notionData = await notionResponse.Content.ReadAsStringAsync();
                Console.WriteLine("Notion verileri başarıyla çekildi.");
                return new JsonResult(new { data = notionData });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Veri çekme hatası: {ex.Message}");
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