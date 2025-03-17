using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            var oauthConfig = _configuration.GetSection($"OAuth:{source}");
            var clientId = oauthConfig["ClientId"];
            var clientSecret = oauthConfig["ClientSecret"];
            var redirectUri = oauthConfig["RedirectUri"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            {
                return BadRequest("OAuth yapılandırması eksik.");
            }

            string tokenUrl;
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
                    break;
                case "fitbit":
                    tokenUrl = "https://api.fitbit.com/oauth2/token";
                    break;
                default:
                    return BadRequest("Geçersiz kaynak");
            }

            using var client = new HttpClient();
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
                return StatusCode((int)response.StatusCode, "Token alma hatası");
            }

            var tokenData = await response.Content.ReadAsStringAsync();
            // tokenData’yı parse et ve veritabanına kaydet
            // Örneğin: { "access_token": "xyz123", "token_type": "Bearer" }

            // Frontend’e bir oturum çerezi gönder
            Response.Cookies.Append("session", "user-session-id", new CookieOptions { HttpOnly = true });
            return Redirect("/");
        }
    }
}