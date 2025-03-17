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
            _logger.LogInformation("Index sayfasý yüklendi.");
            return Page();
        }

        public async Task<IActionResult> OnGetFetchData(string source)
        {
            try
            {
                if (source == "firebase")
                {
                    // Firebase için backend'den veri çekme iþlemi burada olacak
                    // Þimdilik Tasks tablosundan örnek veri çekiyoruz
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
                    // Fitbit aktiviteleri için Tasks veya ayrý bir tablo kullanýlabilir
                    var data = await _context.Tasks.Where(t => t.Source == source).ToListAsync();
                    return new JsonResult(data);
                }
                return new JsonResult(new { message = "Geçersiz kaynak" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Veri çekme hatasý: {source}");
                return StatusCode(500, "Veri çekme hatasý");
            }
        }
    }
}