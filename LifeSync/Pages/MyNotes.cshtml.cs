using backend.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace LifeSync.Pages
{
    public class MyNotesModel : PageModel
    {
        private readonly LifeSyncDbContext _context;

        public MyNotesModel(LifeSyncDbContext context)
        {
            _context = context;
        }

        public List<Note> Notes { get; set; } = new();

        [BindProperty]
        public string NoteTitle { get; set; } = "";

        [BindProperty]
        public string NoteTag { get; set; } = "";

        [BindProperty]
        public string NoteContent { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            Notes = await _context.Notes
                .Where(n => n.Source == "lifesync" && n.UserId == user.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrWhiteSpace(NoteTitle) || string.IsNullOrWhiteSpace(NoteContent) || string.IsNullOrWhiteSpace(NoteTag))
            {
                ModelState.AddModelError(string.Empty, "Tüm alanlar zorunludur.");
                await OnGetAsync();
                return Page();
            }

            var fullContent = $"{NoteTitle} | {NoteTag} | {NoteContent}";

            _context.Notes.Add(new Note
            {
                Id = Guid.NewGuid(),
                Content = fullContent,
                CreatedAt = DateTime.UtcNow,
                Source = "lifesync",
                UserId = user.UserId
            });

            await _context.SaveChangesAsync();
            return RedirectToPage("/MyNotes");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.Source == "lifesync" && n.UserId == user.UserId);
            if (note != null)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/MyNotes");
        }
    }
}
