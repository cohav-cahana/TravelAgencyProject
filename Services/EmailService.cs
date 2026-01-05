using System.Net;
using System.Net.Mail;

namespace TravelAgencyProject.Services
{
    public class EmailService
    {
        // Simple function to send email
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return;
            }
            // For now, this is the basic setup for a "Postman".
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                // Replace with your real email and an "App Password"
                Credentials = new NetworkCredential("cohav1085@gmail.com", "gorzppcggkfndwyf")
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("cohav1085@gmail.com"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}