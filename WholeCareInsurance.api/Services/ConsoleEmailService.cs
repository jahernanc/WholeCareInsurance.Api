namespace WholeCareInsurance.api.Services
{
    // Fallback usado cuando Brevo:ApiKey no está configurado (dev local sin cuenta real).
    // Loguea el contenido del email en vez de enviarlo — pasar a BrevoEmailService en
    // Test/Prod es solo cuestión de setear la variable de entorno, sin tocar código.
    public class ConsoleEmailService : IEmailService
    {
        private readonly ILogger<ConsoleEmailService> _logger;

        public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            _logger.LogWarning(
                "EMAIL NO ENVIADO (Brevo:ApiKey no configurado) — Para: {ToEmail} | Asunto: {Subject}\n{Body}",
                toEmail, subject, htmlBody);
            return Task.CompletedTask;
        }
    }
}
