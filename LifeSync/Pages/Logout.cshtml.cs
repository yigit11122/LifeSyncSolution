using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
namespace LifeSync.Pages
{
    public class LogoutModel : PageModel
    {
        public void OnGet()
        {
            // ?? Tüm session temizle
            HttpContext.Session.Clear();
        }
    }
}