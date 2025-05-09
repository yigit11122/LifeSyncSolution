using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace LifeSync.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        [Required]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        public string Password { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public void OnGet()
        {
            Console.WriteLine("✅ OnGet() çalıştı (Sayfa açıldı).");

            if (TempData["SignUpSuccess"] != null)
            {
                Message = TempData["SignUpSuccess"]?.ToString() ?? string.Empty;
            }
        }

        public IActionResult OnPost()
        {
            Console.WriteLine("🚀 OnPost() metodu çalıştı!");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❗ ModelState geçersiz!");
                Message = "Lütfen tüm alanları doldurun.";
                return Page();
            }

            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("LifeSyncDbContext"));
                connection.Open();

                var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM \"Users\" WHERE \"Email\" = @Email AND \"Password\" = @Password",
                    connection
                );
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@Password", Password);

                var resultObj = cmd.ExecuteScalar();

                if (resultObj != null && resultObj is long result && result > 0)
                {
                    Console.WriteLine("✅ Giriş başarılı! Sayfa yönlendiriliyor.");
                    HttpContext.Session.SetString("UserEmail", Email);
                    return RedirectToPage("/LoginSuccess");
                }
                else
                {
                    Console.WriteLine("❌ Giriş başarısız!");
                    Message = "Email veya şifre hatalı.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 Hata oluştu: " + ex.Message);
                Message = "Hata oluştu: " + ex.Message;
                return Page();
            }
        }
    }
}
