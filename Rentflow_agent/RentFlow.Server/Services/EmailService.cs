using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RentFlow.Server.Services
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string to, string subject, string body)
        {
            // For the demo, we mock the email service to avoid sending real emails.
            _logger.LogInformation("===============================================");
            _logger.LogInformation($"MOCK EMAIL SENT TO: {to}");
            _logger.LogInformation($"SUBJECT: {subject}");
            _logger.LogInformation($"BODY:\n{body}");
            _logger.LogInformation("===============================================");
            
            return Task.CompletedTask;
        }
    }
}
