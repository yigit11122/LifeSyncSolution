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
        public List<Reminder> Reminders { get; set; } = new(); // 🆕 Reminder listesi eklendi

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

        [BindProperty]
        public DateTime? ReminderDate { get; set; }

        public async Task OnGetAsync()
        {
            var userId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48");

            Tasks = await _context.Tasks
                .Where(t => t.Source == "lifesync-task" && t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            Reminders = await _context.Reminders
                .Where(r => r.UserId == userId)
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
            if (Request.Form.TryGetValue("TaskDueDate", out var dueValue) && !string.IsNullOrWhiteSpace(dueValue))
            {
                if (DateTime.TryParse(dueValue, out var parsedDue))
                    parsedDueDate = DateTime.SpecifyKind(parsedDue, DateTimeKind.Local).ToUniversalTime();
            }

            DateTime? parsedReminder = null;
            if (Request.Form.TryGetValue("ReminderDate", out var remValue) && !string.IsNullOrWhiteSpace(remValue))
            {
                if (DateTime.TryParse(remValue, out var parsedRem))
                    parsedReminder = DateTime.SpecifyKind(parsedRem, DateTimeKind.Local).ToUniversalTime();
            }


            var contentFormatted = $"{TaskTitle} | {TaskTag} | {TaskContent}";

            var taskId = Guid.NewGuid();
            var userId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48");

            var task = new TaskItem
            {
                Id = taskId,
                Content = contentFormatted,
                CreatedAt = DateTime.UtcNow,
                DueDate = parsedDueDate,
                Source = "lifesync-task",
                Completed = false,
                UserId = userId
            };

            _context.Tasks.Add(task);

            if (parsedReminder.HasValue)
            {
                var reminder = new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = contentFormatted,
                    ScheduledAt = parsedReminder.Value,
                    UserId = userId
                };

                _context.Reminders.Add(reminder);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);

                // Eşleşen reminder varsa sil
                var reminder = await _context.Reminders
                    .FirstOrDefaultAsync(r => r.Title == task.Content && r.UserId == task.UserId);
                if (reminder != null)
                    _context.Reminders.Remove(reminder);

                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
