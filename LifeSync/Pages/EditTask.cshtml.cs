using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using backend.models;
using Microsoft.EntityFrameworkCore;

namespace LifeSync.Pages
{
    public class EditTaskModel : PageModel
    {
        private readonly LifeSyncDbContext _context;

        public EditTaskModel(LifeSyncDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TaskItem? Task { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.Source == "lifesync-task");
            if (Task == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id, string Title, string Tag, string Content, string? DueDate)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.Source == "lifesync-task");
            if (task == null)
                return NotFound();

            task.Content = $"{Title} | {Tag} | {Content}";

            if (!string.IsNullOrEmpty(DueDate) && DateTime.TryParse(DueDate, out var dt))
                task.DueDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            else
                task.DueDate = null;

            // ✅ Checkbox kontrolü burada (bu çözüm her zaman çalışır)
            task.Completed = Request.Form["Completed"] == "on";

            await _context.SaveChangesAsync();
            return RedirectToPage("/MyTasks");
        }
    }
}
