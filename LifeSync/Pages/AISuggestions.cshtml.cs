using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LifeSync.Pages
{
    public class AISuggestionsModel : PageModel
    {
        public string Title { get; set; } = "LifeSync - AI Task Recommender";

        public void OnGet()
        {
        }
    }
}
