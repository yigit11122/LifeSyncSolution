using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using backend.models;
using Microsoft.AspNetCore.Http;

namespace LifeSync.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly LifeSyncDbContext _context;

        public SettingsModel(LifeSyncDbContext context)
        {
            _context = context;
        }

        public Dictionary<string, ConnectionStatus> IntegrationStatuses { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // 🔒 Oturum kontrolü
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

            var sources = new[] { "notion", "todoist" };

            foreach (var source in sources)
            {
                var latestToken = await _context.OAuthTokens
                    .Where(t => t.Source.ToLower() == source && t.UserId == user.UserId)
                    .OrderByDescending(t => t.ExpiryDate)
                    .FirstOrDefaultAsync();

                IntegrationStatuses[source] = latestToken != null
                    ? new ConnectionStatus
                    {
                        IsConnected = true,
                        ExpiryDate = latestToken.ExpiryDate,
                        ConnectedAt = latestToken.ExpiryDate.AddSeconds(-3600)
                    }
                    : new ConnectionStatus
                    {
                        IsConnected = false,
                        ExpiryDate = null,
                        ConnectedAt = null
                    };
            }

            return Page();
        }

        public class ConnectionStatus
        {
            public bool IsConnected { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public DateTime? ConnectedAt { get; set; }
        }
    }
}
