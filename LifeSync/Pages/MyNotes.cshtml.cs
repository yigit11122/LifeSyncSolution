using backend.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

        public async Task OnGetAsync()
        {
            Notes = await _context.Notes
                .Where(n => n.Source == "lifesync")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
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
                UserId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48") // varsayýlan kullanýcý
            });

            await _context.SaveChangesAsync();

            return RedirectToPage("/MyNotes");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.Source == "lifesync");
            if (note != null)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/MyNotes");
        }
    }
}
