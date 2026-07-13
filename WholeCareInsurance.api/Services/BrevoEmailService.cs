using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WholeCareInsurance.api.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public BrevoEmailService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"] ?? "WholeCare Insurance", email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = toEmail } },
                subject,
                htmlContent = htmlBody
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", _config["Brevo:ApiKey"]);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
