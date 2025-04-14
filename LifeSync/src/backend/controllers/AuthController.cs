using backend.models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LifeSync.Controllers
{
    [Route("auth/{source}")]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("connect")]
        public IActionResult Connect(string source, string state)
        {
            var oauthConfig = _configuration.GetSection($"OAuth:{source}");
            var clientId = oauthConfig["ClientId"];
            var redirectUri = oauthConfig["RedirectUri"];
            var scope = oauthConfig["Scope"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            {
                return BadRequest("OAuth yapılandırması eksik.");
            }

            string authUrl;
            switch (source.ToLower())
            {
                case "todoist":
                    authUrl = $"https://todoist.com/oauth/authorize?client_id={clientId}&scope={scope}&state={state}&redirect_uri={redirectUri}&response_type=code";
                    break;
                case "googlecalendar":
                    authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={state}";
                    break;
                case "notion":
                    authUrl = $"https://api.notion.com/v1/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&state={state}";
                    break;
                case "fitbit":
                    authUrl = $"https://www.fitbit.com/oauth2/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={state}";
                    break;
                default:
                    return BadRequest("Geçersiz kaynak");
            }

            return Redirect(authUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string source, string code, string state)
        {
            if (string.IsNullOrEmpty(code))
            {
                Console.WriteLine("Hata: Yetkilendirme kodu (code) boş.");
                return BadRequest("Yetkilendirme kodu eksik.");
            }

            var oauthConfig = _configuration.GetSection($"OAuth:{source}");
            var clientId = oauthConfig["ClientId"];
            var clientSecret = oauthConfig["ClientSecret"];
            var redirectUri = oauthConfig["RedirectUri"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            {
                return BadRequest("OAuth yapılandırması eksik.");
            }

            string tokenUrl;
            using var client = new HttpClient();
            switch (source.ToLower())
            {
                case "todoist":
                    tokenUrl = "https://todoist.com/oauth/access_token";
                    break;
                case "googlecalendar":
                    tokenUrl = "https://oauth2.googleapis.com/token";
                    break;
                case "notion":
                    tokenUrl = "https://api.notion.com/v1/oauth/token";
                    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
                    break;
                case "fitbit":
                    tokenUrl = "https://api.fitbit.com/oauth2/token";
                    break;
                default:
                    return BadRequest("Geçersiz kaynak");
            }

            var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            }));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token alma hatası: {error}");
                return StatusCode((int)response.StatusCode, "Token alma hatası");
            }

            var tokenData = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Token alındı: {tokenData}");

            // Düzeltme: JSON'ı JsonDocument ile ayrıştır
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(tokenData);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("access_token", out var accessTokenElement) || accessTokenElement.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                Console.WriteLine("Token parse hatası: access_token bulunamadı.");
                return StatusCode(500, "Token parse hatası");
            }

            var accessToken = accessTokenElement.GetString();
            string? refreshToken = null;
            if (root.TryGetProperty("refresh_token", out var refreshTokenElement) && refreshTokenElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                refreshToken = refreshTokenElement.GetString();
            }

            int expiresIn = 3600; // Varsayılan 1 saat
            if (root.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                expiresIn = expiresInElement.GetInt32();
            }

            var expiryDate = DateTime.UtcNow.AddSeconds(expiresIn);

            try
            {
                using (var scope = this.HttpContext.RequestServices.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<LifeSyncDbContext>();
                    context.OAuthTokens.Add(new OAuthToken
                    {
                        Source = source,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        ExpiryDate = expiryDate,
                        UserId = Guid.Parse("35529975-876b-4bf6-b919-cafaa64eee48")
                    });
                    await context.SaveChangesAsync();
                    Console.WriteLine("Token veritabanına başarıyla kaydedildi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Veritabanına kaydetme hatası: {ex.Message}");
                return StatusCode(500, "Veritabanına kaydetme hatası: " + ex.Message);
            }

            Response.Cookies.Append("session", "user-session-id", new CookieOptions { HttpOnly = true });

            // Yetkilendirme sonrası Index sayfasına source ve code parametreleriyle yönlendirme
            var redirectUrl = $"/?source={source}&code={code}&state={state}";
            return Redirect(redirectUrl);
        }
    }
}