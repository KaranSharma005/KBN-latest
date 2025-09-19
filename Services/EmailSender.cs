using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using KBN.Models;

namespace KBN.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettingsModal _emailSettings;
        public EmailSender(IOptions<EmailSettingsModal> options)
        {
            _emailSettings = options.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                using (SmtpClient client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                {
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                    client.EnableSsl = true;

                    MailMessage mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.FromEmail),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex}");
            }
        }
    }
}
