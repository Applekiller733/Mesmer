using FluentAssertions;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class EmailServiceTests
    {
        [Fact]
        public void Constructor_BuildsService()
        {
            var service = new EmailService(TestSettings.AppSettings());
            service.Should().NotBeNull();
        }

        [Fact]
        public void Send_UnreachableHost_Throws()
        { 
            var settings = Microsoft.Extensions.Options.Options.Create(
                new SongAppApi.Helpers.AppSettings
                {
                    Secret = "x",
                    RefreshTokenTTL = 1,
                    EmailFrom = "test@example.com",
                    SmtpHost = "127.0.0.1",
                    SmtpPort = 1, // privileged port that won't accept
                    SmtpUser = "u",
                    SmtpPass = "p",
                });
            var service = new EmailService(settings);

            var act = () => service.Send("to@example.com", "subj", "<p>hi</p>");

            act.Should().Throw<Exception>();
        }
    }
}
