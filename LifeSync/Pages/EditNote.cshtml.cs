using backend.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LifeSync.Pages
{
    public class EditNoteModel : PageModel
    {
        private readonly LifeSyncDbContext _context;

        public EditNoteModel(LifeSyncDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Note Note { get; set; } = new();

        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Tags { get; set; } = "";

        [BindProperty]
        public string Body { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.Source == "lifesync");
            if (Note == null) return NotFound();

            var parts = Note.Content.Split(" | ");
            Title = parts.Length > 0 ? parts[0] : "";
            Tags = parts.Length > 1 ? parts[1] : "";
            Body = parts.Length > 2 ? parts[2] : "";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == Note.Id && n.Source == "lifesync");
            if (note == null) return NotFound();

            note.Content = $"{Title} | {Tags} | {Body}";
            await _context.SaveChangesAsync();

            return RedirectToPage("/MyNotes");
        }
    }
}
