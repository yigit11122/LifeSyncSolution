using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace LifeSync.Pages
{
    public class AlSuggestionModel : PageModel
    {
        private readonly IWebHostEnvironment _env;

        public AlSuggestionModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Body'den veriyi oku
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<AIRequest>(body);

            if (request == null)
                return BadRequest("İstek verisi boş geldi!");

            var suggestions = await LoadSuggestionsAsync();

            // Kategoriye göre filtrele
            var filtered = suggestions
                .Where(x => string.IsNullOrWhiteSpace(request.Category) ||
                            (x.Category != null && x.Category.Contains(request.Category, StringComparison.OrdinalIgnoreCase)))
                .Take(5) // En fazla 5 tane gönder
                .ToList();

            return new JsonResult(filtered);
        }

        private async Task<List<Suggestion>> LoadSuggestionsAsync()
        {
            var path = Path.Combine(_env.ContentRootPath, "pre_written_tasks.json");

            if (!System.IO.File.Exists(path))
                return new List<Suggestion>();

            var json = await System.IO.File.ReadAllTextAsync(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var suggestions = JsonSerializer.Deserialize<List<Suggestion>>(json, options);
            return suggestions ?? new List<Suggestion>();
        }
    }

    public class AIRequest
    {
        public string Task { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
    }

    public class Suggestion
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Estimated_Time { get; set; } = "";
    }
}
