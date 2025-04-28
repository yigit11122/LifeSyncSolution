using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LifeSync.Pages
{
    public class TaskRecommenderModel : PageModel
    {
        public string Title { get; set; }

        public TaskRecommenderModel()
        {
            Title = "LifeSync - AI Task Recommender";
        }

        public IActionResult OnPostSuggest(string task, string description, string category)
        {
            // Example logic for handling POST request
            var suggestions = GetSuggestions(task, description, category);

            // Return JSON response with recommendations
            return new JsonResult(suggestions);
        }

        private List<Suggestion> GetSuggestions(string task, string description, string category)
        {
            // Example dummy suggestions logic
            var suggestions = new List<Suggestion>
            {
                new Suggestion { Name = "Study for Exam", Category = category, Priority = "High" },
                new Suggestion { Name = "Complete Project", Category = category, Priority = "Medium" }
            };

            return suggestions;
        }
    }

    public class Suggestion
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
    }
}
