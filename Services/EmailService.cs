using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace TravelAgencyProject.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        // Dependency Injection to access configuration values
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Sends an email asynchronously using Gmail SMTP settings.
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Basic validation for the recipient address
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            try
            {
                // Configuration keys for Gmail SMTP
                var smtpServer = "smtp.gmail.com";
                var smtpPort = 587;

                // Securely fetch credentials from User Secrets or App Settings
                var senderEmail = _configuration["EmailSettings:SmtpUser"];
                var appPassword = _configuration["EmailSettings:SmtpPass"];

                // SMTP client configuration
                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(senderEmail, appPassword);

                    // Email content configuration
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true // Enables HTML formatting in the email body
                    };
                    mailMessage.To.Add(toEmail);

                    // Execute the sending process
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception)
            {
                // In production, errors are typically logged to a file.
                // We handle the exception here to ensure the application remains stable 
                // even if the email delivery fails.
            }
        }
    }
}