namespace SongAppApi.Services
{

    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Options;
    using MimeKit;
    using MimeKit.Text;
    using SongAppApi.Helpers;

    public interface IEmailService
    {
        void Send(string to, string subject, string html, string from = null);
    }
    public class EmailService : IEmailService
    {
        private readonly AppSettings _settings;

        public EmailService(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;
        }
        public void Send(string to, string subject, string html, string from = null)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from ?? _settings.EmailFrom));
            //email.From.Add(MailboxAddress.Parse(_settings.EmailFrom));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = html };

            using var smtp = new SmtpClient();
            smtp.Connect(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            smtp.Authenticate(_settings.SmtpUser, _settings.SmtpPass);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}
