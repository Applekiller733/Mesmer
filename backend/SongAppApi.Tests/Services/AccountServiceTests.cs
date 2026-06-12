using FluentAssertions;
using Moq;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Accounts;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class AccountServiceTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<IJwtUtils> _jwt;
        private readonly Mock<IEmailService> _email;
        private readonly AccountService _service;

        public AccountServiceTests()
        {
            _context = TestDbContextFactory.Create();
            var mapper = TestMapperFactory.Create(_context);
            _jwt = new Mock<IJwtUtils>();
            _email = new Mock<IEmailService>();

            _jwt.Setup(j => j.GenerateJwtToken(It.IsAny<Account>())).Returns("test-jwt");
            _jwt.Setup(j => j.GenerateRefreshToken(It.IsAny<string>()))
                .Returns((string ip) => new RefreshToken
                {
                    Token = Guid.NewGuid().ToString("N"),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    CreatedByIp = ip,
                });

            _service = new AccountService(
                _context, _jwt.Object, mapper, TestSettings.AppSettings(), _email.Object);
        }

        public void Dispose() => _context.Dispose();


        [Fact]
        public void Authenticate_ValidCredentials_ReturnsResponse()
        {
            var acc = EntityBuilder.NewAccount(
                email: "user@example.com",
                passwordHash: BCrypt.Net.BCrypt.HashPassword("Password!1"),
                verified: true);
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            var result = _service.Authenticate(
                new AuthenticateRequest { Email = "user@example.com", Password = "Password!1" },
                "127.0.0.1");

            result.JwtToken.Should().Be("test-jwt");
            result.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Authenticate_WrongPassword_Throws()
        {
            var acc = EntityBuilder.NewAccount(
                email: "user@example.com",
                passwordHash: BCrypt.Net.BCrypt.HashPassword("CorrectPwd!"),
                verified: true);
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            var act = () => _service.Authenticate(
                new AuthenticateRequest { Email = "user@example.com", Password = "WrongPwd" },
                "127.0.0.1");

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Authenticate_UnverifiedAccount_Throws()
        {
            var acc = EntityBuilder.NewAccount(
                email: "user@example.com",
                passwordHash: BCrypt.Net.BCrypt.HashPassword("Password!1"),
                verified: false);
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            var act = () => _service.Authenticate(
                new AuthenticateRequest { Email = "user@example.com", Password = "Password!1" },
                "127.0.0.1");

            act.Should().Throw<AppException>();
        }

        [Fact]
        public void Authenticate_UnknownEmail_Throws()
        {
            var act = () => _service.Authenticate(
                new AuthenticateRequest { Email = "nobody@example.com", Password = "x" },
                "127.0.0.1");

            act.Should().Throw<AppException>();
        }


        [Fact]
        public void Register_CreatesAccountAndSendsVerificationEmail()
        {
            _service.Register(new RegisterRequest
            {
                UserName = "newuser",
                Email = "new@example.com",
                Password = "Password!1",
                ConfirmPassword = "Password!1",
                AcceptTerms = true,
            }, origin: "http://localhost");

            _context.Accounts.Any(a => a.Email == "new@example.com").Should().BeTrue();
            _email.Verify(e => e.Send(
                "new@example.com",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Register_FirstAccount_IsAdmin()
        {
            _service.Register(new RegisterRequest
            {
                UserName = "first",
                Email = "first@example.com",
                Password = "Password!1",
                ConfirmPassword = "Password!1",
                AcceptTerms = true,
            }, origin: "http://localhost");

            var acc = _context.Accounts.Single(a => a.Email == "first@example.com");
            acc.Role.Should().Be(Role.Admin);
        }

        [Fact]
        public void Register_SecondAccount_IsUser()
        {
            _context.Accounts.Add(EntityBuilder.NewAccount(role: Role.Admin));
            _context.SaveChanges();

            _service.Register(new RegisterRequest
            {
                UserName = "second",
                Email = "second@example.com",
                Password = "Password!1",
                ConfirmPassword = "Password!1",
                AcceptTerms = true,
            }, origin: "http://localhost");

            var acc = _context.Accounts.Single(a => a.Email == "second@example.com");
            acc.Role.Should().Be(Role.User);
        }

        [Fact]
        public void Register_DuplicateEmail_SendsAlreadyRegisteredEmailAndReturns()
        {
            // the service uses a deliberate non-enumeration pattern: don't
            // throw on duplicate, send a different email instead.
            _context.Accounts.Add(EntityBuilder.NewAccount(email: "dup@example.com"));
            _context.SaveChanges();

            _service.Register(new RegisterRequest
            {
                UserName = "x",
                Email = "dup@example.com",
                Password = "Password!1",
                ConfirmPassword = "Password!1",
                AcceptTerms = true,
            }, origin: "http://localhost");

            // only the original record exists.
            _context.Accounts.Count(a => a.Email == "dup@example.com").Should().Be(1);
            // an already registered email was sent.
            _email.Verify(e => e.Send(
                "dup@example.com",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Register_HashesPassword()
        {
            _service.Register(new RegisterRequest
            {
                UserName = "x",
                Email = "x@example.com",
                Password = "Password!1",
                ConfirmPassword = "Password!1",
                AcceptTerms = true,
            }, origin: "http://localhost");

            var acc = _context.Accounts.Single(a => a.Email == "x@example.com");
            acc.PasswordHash.Should().NotBe("Password!1");
            BCrypt.Net.BCrypt.Verify("Password!1", acc.PasswordHash).Should().BeTrue();
        }


        [Fact]
        public void VerifyEmail_ValidToken_VerifiesAccount()
        {
            var acc = EntityBuilder.NewAccount(verified: false);
            acc.VerificationToken = "test-token";
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            _service.VerifyEmail("test-token");

            var reloaded = _context.Accounts.Single(a => a.Id == acc.Id);
            reloaded.Verified.Should().NotBeNull();
            reloaded.VerificationToken.Should().BeNull();
        }

        [Fact]
        public void VerifyEmail_InvalidToken_Throws()
        {
            var act = () => _service.VerifyEmail("not-a-real-token");
            act.Should().Throw<AppException>();
        }


        [Fact]
        public void ForgotPassword_UnknownEmail_NoOp()
        {
            // returns silently to avoid email enumeration.
            var act = () => _service.ForgotPassword(
                new ForgotPasswordRequest { Email = "nobody@example.com" },
                origin: "http://localhost");

            act.Should().NotThrow();
            _email.Verify(e => e.Send(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void ForgotPassword_KnownEmail_SendsResetEmailAndSetsToken()
        {
            var acc = EntityBuilder.NewAccount(email: "user@example.com");
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            _service.ForgotPassword(
                new ForgotPasswordRequest { Email = "user@example.com" },
                origin: "http://localhost");

            var reloaded = _context.Accounts.Single(a => a.Id == acc.Id);
            reloaded.ResetToken.Should().NotBeNullOrEmpty();
            reloaded.ResetTokenExpires.Should().BeAfter(DateTime.UtcNow);
            _email.Verify(e => e.Send(
                "user@example.com",
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }


        [Fact]
        public void ResetPassword_ValidToken_UpdatesPasswordAndClearsToken()
        {
            var acc = EntityBuilder.NewAccount();
            acc.ResetToken = "valid-reset";
            acc.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            _service.ResetPassword(new ResetPasswordRequest
            {
                Token = "valid-reset",
                Password = "NewPassword!1",
                ConfirmPassword = "NewPassword!1",
            });

            var reloaded = _context.Accounts.Single(a => a.Id == acc.Id);
            reloaded.ResetToken.Should().BeNull();
            BCrypt.Net.BCrypt.Verify("NewPassword!1", reloaded.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public void ResetPassword_ExpiredToken_Throws()
        {
            var acc = EntityBuilder.NewAccount();
            acc.ResetToken = "expired";
            acc.ResetTokenExpires = DateTime.UtcNow.AddHours(-1);
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            var act = () => _service.ResetPassword(new ResetPasswordRequest
            {
                Token = "expired",
                Password = "x",
                ConfirmPassword = "x",
            });

            act.Should().Throw<AppException>();
        }


        [Fact]
        public void GetById_ReturnsAccount()
        {
            var acc = EntityBuilder.NewAccount();
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            var result = _service.GetById(acc.Id.ToString());

            result.Id.Should().Be(acc.Id.ToString());
        }

        [Fact]
        public void GetById_Missing_Throws()
        {
            var act = () => _service.GetById(Guid.NewGuid().ToString());
            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void GetAll_ReturnsAll()
        {
            _context.Accounts.AddRange(
                EntityBuilder.NewAccount(),
                EntityBuilder.NewAccount(),
                EntityBuilder.NewAccount());
            _context.SaveChanges();

            _service.GetAll().Should().HaveCount(3);
        }


        [Fact]
        public void Create_AddsAccountWithHashedPassword()
        {
            var result = _service.Create(new CreateRequest
            {
                UserName = "created",
                Email = "created@example.com",
                Password = "Password!1",
                ConfirmPassword = "Password!1",
                Role = Role.User.ToString(),
            });

            result.Email.Should().Be("created@example.com");
            var stored = _context.Accounts.Single(a => a.Email == "created@example.com");
            stored.PasswordHash.Should().NotBe("Password!1");
        }

        [Fact]
        public void Create_DuplicateEmail_Throws()
        {
            _context.Accounts.Add(EntityBuilder.NewAccount(email: "taken@example.com"));
            _context.SaveChanges();

            var act = () => _service.Create(new CreateRequest
            {
                UserName = "x",
                Email = "taken@example.com",
                Password = "Password!1",
                ConfirmPassword = "Password!1",
                Role = Role.User.ToString(),
            });

            act.Should().Throw<AppException>();
        }


        [Fact]
        public void Delete_RemovesAccount()
        {
            var acc = EntityBuilder.NewAccount();
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            _service.Delete(acc.Id.ToString());

            _context.Accounts.Any(a => a.Id == acc.Id).Should().BeFalse();
        }

        [Fact]
        public void Delete_Missing_Throws()
        {
            var act = () => _service.Delete(Guid.NewGuid().ToString());
            act.Should().Throw<KeyNotFoundException>();
        }
    }
}
