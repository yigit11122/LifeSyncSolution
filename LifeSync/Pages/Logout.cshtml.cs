using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
namespace LifeSync.Pages
{
    public class LogoutModel : PageModel
    {
        public void OnGet()
        {
            // ?? T�m session temizle
            HttpContext.Session.Clear();
        }
    }
}