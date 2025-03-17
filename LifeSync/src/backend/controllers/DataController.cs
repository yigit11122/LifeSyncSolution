using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using backend.models;
using Microsoft.EntityFrameworkCore;
using System;

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
                foreach (var item in request.Data)
                {
                    switch (request.Source.ToLower())
                    {
                        case "todoist":
                            _context.Tasks.Add(new TaskItem
                            {
                                Id = Guid.Parse(item.Id),
                                Content = item.Content,
                                DueDate = item.DueDate != null ? DateTime.Parse(item.DueDate) : null,
                                Completed = item.Completed,
                                Source = "todoist"
                            });
                            break;
                        case "googlecalendar":
                            _context.Events.Add(new Event
                            {
                                Id = Guid.Parse(item.Id),
                                Summary = item.Content,
                                StartDate = DateTime.Parse(item.StartDate),
                                Source = "googleCalendar"
                            });
                            break;
                        case "notion":
                            _context.Notes.Add(new Note
                            {
                                Id = Guid.Parse(item.Id),
                                Content = item.Content,
                                CreatedAt = DateTime.Parse(item.CreatedAt),
                                Source = "notion"
                            });
                            break;
                        case "fitbit":
                        case "lifesync":
                            _context.Tasks.Add(new TaskItem
                            {
                                Id = Guid.Parse(item.Id),
                                Content = item.Content,
                                DueDate = item.CreatedAt != null ? DateTime.Parse(item.CreatedAt) : null,
                                Source = request.Source
                            });
                            break;
                    }
                }
                await _context.SaveChangesAsync();
                return Ok(new { message = "Veri kaydedildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Veri kaydetme hatası: {ex.Message}");
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
        public string DueDate { get; set; }
        public string StartDate { get; set; }
        public string CreatedAt { get; set; }
        public bool Completed { get; set; }
    }
}