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

        public Reminder? Reminder { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.Source == "lifesync-task");
            if (Task == null)
                return NotFound();

            Reminder = await _context.Reminders.FirstOrDefaultAsync(r => r.UserId == Task.UserId && r.Title.Contains(Task.Id.ToString()));

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id, string Title, string Tag, string Content, string? DueDate, string? ReminderDate)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.Source == "lifesync-task");
            if (task == null)
                return NotFound();

            task.Content = $"{Title} | {Tag} | {Content}";

            if (!string.IsNullOrEmpty(DueDate) && DateTime.TryParse(DueDate, out var dt))
                task.DueDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            else
                task.DueDate = null;

            task.Completed = Request.Form["Completed"] == "on";

            // 🔔 Anımsatıcı güncelle
            var existingReminder = await _context.Reminders.FirstOrDefaultAsync(r => r.UserId == task.UserId && r.Title.Contains(task.Id.ToString()));
            if (!string.IsNullOrWhiteSpace(ReminderDate) && DateTime.TryParse(ReminderDate, out var reminderDt))
            {
                reminderDt = DateTime.SpecifyKind(reminderDt, DateTimeKind.Utc);

                if (existingReminder != null)
                {
                    existingReminder.ScheduledAt = reminderDt;
                }
                else
                {
                    _context.Reminders.Add(new Reminder
                    {
                        Id = Guid.NewGuid(),
                        Title = $"Reminder for task {task.Id}",
                        ScheduledAt = reminderDt,
                        UserId = task.UserId
                    });
                }
            }
            else if (existingReminder != null)
            {
                // Eğer reminder alanı boşsa ve daha önce reminder varsa, onu silelim
                _context.Reminders.Remove(existingReminder);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/MyTasks");
        }
    }
}
