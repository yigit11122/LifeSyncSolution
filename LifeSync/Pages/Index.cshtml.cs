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
            _logger.LogInformation("Index sayfası yüklendi.");
            return Page();
        }

        public async Task<IActionResult> OnGetFetchData(string source)
        {
            _logger.LogInformation($"Veri çekme isteği alındı: {source ?? "null"}");
            if (string.IsNullOrEmpty(source))
                return new JsonResult(new { error = "Kaynak parametresi eksik" });

            try
            {
                List<object> data = source.ToLower() switch
                {
                    "todoist" => (await _context.Tasks.Where(t => t.Source == source).ToListAsync()).Cast<object>().ToList(),
                    "googlecalendar" => (await _context.Events.Where(e => e.Source == source).ToListAsync()).Cast<object>().ToList(),
                    "notion" => (await _context.Notes.Where(n => n.Source == source).ToListAsync()).Cast<object>().ToList(),
                    "fitbit" => (await _context.Tasks.Where(t => t.Source == source).ToListAsync()).Cast<object>().ToList(),
                    "lifesync" => (await _context.Notes.Where(n => n.Source == source).ToListAsync()).Cast<object>().ToList(),
                    _ => throw new Exception("Geçersiz kaynak")
                };

                _logger.LogInformation($"{source} verileri çekildi: {data.Count} kayıt.");
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Veri çekme hatası: {source}");
                return StatusCode(500, new { error = $"Veri çekme hatası: {ex.Message}" });
            }
        }
    }
}
