using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using backend.models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LifeSync.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly LifeSyncDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, LifeSyncDbContext context)
        {
            _logger = logger;
            _context = context ?? throw new ArgumentNullException(nameof(context), "LifeSyncDbContext null olamaz.");
        }

        public IActionResult OnGet()
        {
            _logger.LogInformation("Index sayfas� y�klendi.");
            return Page();
        }

        public async Task<IActionResult> OnGetFetchData(string source)
        {
            _logger.LogInformation($"Veri �ekme iste�i al�nd�: {source ?? "null"}");
            if (string.IsNullOrEmpty(source))
            {
                _logger.LogWarning("Kaynak parametresi eksik.");
                return new JsonResult(new { error = "Kaynak parametresi eksik" });
            }

            try
            {
                List<object> data = new List<object>();
                switch (source.ToLower())
                {
                    case "todoist":
                        data = (await _context.Tasks.Where(t => t.Source == source).ToListAsync()).Cast<object>().ToList();
                        break;
                    case "googlecalendar":
                        data = (await _context.Events.Where(e => e.Source == source).ToListAsync()).Cast<object>().ToList();
                        break;
                    case "notion":
                        data = (await _context.Notes.Where(n => n.Source == source).ToListAsync()).Cast<object>().ToList();
                        break;
                    case "fitbit":
                        data = (await _context.Tasks.Where(t => t.Source == source).ToListAsync()).Cast<object>().ToList();
                        break;
                    case "lifesync":
                        data = (await _context.Tasks.Where(t => t.Source == source).ToListAsync()).Cast<object>().ToList();
                        break;
                    default:
                        _logger.LogWarning($"Ge�ersiz veri kayna��: {source}");
                        return new JsonResult(new { error = "Ge�ersiz kaynak" });
                }

                _logger.LogInformation($"{source} verileri �ekildi: {data.Count} kay�t.");
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Veri �ekme hatas�: {source}");
                return StatusCode(500, new { error = $"Veri �ekme hatas�: {ex.Message}" });
            }
        }
    }
}