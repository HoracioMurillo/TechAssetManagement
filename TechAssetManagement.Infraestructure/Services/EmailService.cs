using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using TechAssetManagement.Core.Interfaces;

namespace TechAssetManagement.Infraestructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string message)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["SenderEmail"], emailSettings["SenderName"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(to);

            using var client = new SmtpClient(emailSettings["Server"], int.Parse(emailSettings["Port"]))
            {
                Credentials = new NetworkCredential(emailSettings["Username"], emailSettings["Password"]),
                EnableSsl = true,
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}