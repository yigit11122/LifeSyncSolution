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
            _context = context;
        }

        public IActionResult OnGet()
        {
            _logger.LogInformation("Index sayfas� y�klendi.");
            return Page();
        }

        public async Task<IActionResult> OnGetFetchData(string source)
        {
            try
            {
                if (source == "firebase")
                {
                    // Firebase i�in backend'den veri �ekme i�lemi burada olacak
                    // �imdilik Tasks tablosundan �rnek veri �ekiyoruz
                    var data = await _context.Tasks.Where(t => t.Source == source).ToListAsync();
                    return new JsonResult(data);
                }
                else if (source == "todoist")
                {
                    var data = await _context.Tasks.Where(t => t.Source == source).ToListAsync();
                    return new JsonResult(data);
                }
                else if (source == "googleCalendar")
                {
                    var data = await _context.Events.Where(e => e.Source == source).ToListAsync();
                    return new JsonResult(data);
                }
                else if (source == "notion")
                {
                    var data = await _context.Notes.Where(n => n.Source == source).ToListAsync();
                    return new JsonResult(data);
                }
                else if (source == "fitbit")
                {
                    // Fitbit aktiviteleri i�in Tasks veya ayr� bir tablo kullan�labilir
                    var data = await _context.Tasks.Where(t => t.Source == source).ToListAsync();
                    return new JsonResult(data);
                }
                return new JsonResult(new { message = "Ge�ersiz kaynak" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Veri �ekme hatas�: {source}");
                return StatusCode(500, "Veri �ekme hatas�");
            }
        }
    }
}