using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using backend.models;

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

        public async Task OnGetAsync()
        {
            var sources = new[] { "notion", "todoist" };

            foreach (var source in sources)
            {
                var latestToken = await _context.OAuthTokens
                    .Where(t => t.Source.ToLower() == source)
                    .OrderByDescending(t => t.ExpiryDate)
                    .FirstOrDefaultAsync();

                IntegrationStatuses[source] = latestToken != null
                    ? new ConnectionStatus
                    {
                        IsConnected = true,
                        ExpiryDate = latestToken.ExpiryDate,
                        ConnectedAt = latestToken.ExpiryDate.AddSeconds(-3600) // varsayım: token 1 saatlik
                    }
                    : new ConnectionStatus
                    {
                        IsConnected = false,
                        ExpiryDate = null,
                        ConnectedAt = null
                    };
            }
        }

        public class ConnectionStatus
        {
            public bool IsConnected { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public DateTime? ConnectedAt { get; set; }
        }
    }
}
