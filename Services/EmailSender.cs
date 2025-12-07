using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BookstoreManagement.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(
                    _config["EmailSettings:Email"],
                    _config["EmailSettings:Password"]
                ),
                EnableSsl = true
            };

            // DEBUG LOG
            var fromEmail = _config["EmailSettings:Email"];
            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new Exception("Thiếu cấu hình EmailSettings:Email");


            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Bookstore Management"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}