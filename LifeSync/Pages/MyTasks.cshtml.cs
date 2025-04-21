using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using backend.models;
using Microsoft.EntityFrameworkCore;

namespace LifeSync.Pages
{
    public class MyTasksModel : PageModel
    {
        private readonly LifeSyncDbContext _context;

        public MyTasksModel(LifeSyncDbContext context)
        {
            _context = context;
        }

        public List<TaskItem> Tasks { get; set; } = new();

        [BindProperty]
        public string TaskTitle { get; set; } = "";

        [BindProperty]
        public string TaskTag { get; set; } = "";

        [BindProperty]
        public string TaskContent { get; set; } = "";

        [BindProperty]
        public DateTime? TaskStartDate { get; set; }

        [BindProperty]
        public DateTime? TaskDueDate { get; set; }

        public async Task OnGetAsync()
        {
            Tasks = await _context.Tasks
                .Where(t => t.Source == "lifesync-task")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (string.IsNullOrWhiteSpace(TaskTitle) || string.IsNullOrWhiteSpace(TaskContent))
            {
                ModelState.AddModelError("", "Başlık ve içerik boş olamaz.");
                return Page();
            }

            DateTime? parsedDueDate = null;
            if (Request.Form.TryGetValue("TaskDueDate", out var dateValue) && !string.IsNullOrWhiteSpace(dateValue))
            {
                if (DateTime.TryParse(dateValue, out var parsed))
                    parsedDueDate = DateTime.SpecifyKind(parsed, DateTimeKind.Utc); // 🛠️ Hata burada düzeltildi
            }

            var contentFormatted = $"{TaskTitle} | {TaskTag} | {TaskContent}";

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Content = contentFormatted,
                CreatedAt = DateTime.UtcNow,
                DueDate = parsedDueDate,
                Source = "lifesync-task",
                Completed = false,
                UserId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48")
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
