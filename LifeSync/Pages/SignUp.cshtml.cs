using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace LifeSync.Pages
{
    public class SignUpModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public SignUpModel(IConfiguration configuration)
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
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Message = "Lütfen tüm alanları doldurun.";
                return Page();
            }

            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("LifeSyncDbContext"));
                connection.Open();

                // Email zaten kayıtlı mı kontrol et
                var checkCmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM \"Users\" WHERE \"Email\" = @Email", connection
                );
                checkCmd.Parameters.AddWithValue("@Email", Email);
                var result = checkCmd.ExecuteScalar();

                if (result != null && result is long count && count > 0)
                {
                    Message = "Bu email ile zaten bir hesap mevcut.";
                    return Page();
                }

                // Yeni kullanıcı ekle
                var insertCmd = new NpgsqlCommand(
                    "INSERT INTO \"Users\" (\"Email\", \"Username\", \"Password\") VALUES (@Email, @Username, @Password)", connection
                );
                insertCmd.Parameters.AddWithValue("@Email", Email);
                insertCmd.Parameters.AddWithValue("@Username", Email.Split('@')[0]); // ✅ Username mail ön eki
                insertCmd.Parameters.AddWithValue("@Password", Password);
                insertCmd.ExecuteNonQuery();

                TempData["SignUpSuccess"] = "Kayıt başarıyla tamamlandı. Giriş yapabilirsiniz.";
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                Message = "Hata oluştu: " + ex.Message;
                return Page();
            }
        }
    }
}
