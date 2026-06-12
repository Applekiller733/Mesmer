using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SongAppApi.Controllers;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Accounts;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Controllers
{
    public class AccountsControllerTests
    {
        private readonly Mock<IAccountService> _accountService = new();
        private readonly Mock<IFileService> _fileService = new();
        private readonly AccountsController _controller;

        public AccountsControllerTests()
        {
            _controller = new AccountsController(_accountService.Object, _fileService.Object);
        }
        private void AttachWithIp(Account? account)
        {
            ControllerContextHelper.Attach(_controller, account, remoteIp: "127.0.0.1");
        }


        [Fact]
        public void Authenticate_HappyPath_ReturnsOk()
        {
            AttachWithIp(account: null);
            _accountService.Setup(s => s.Authenticate(It.IsAny<AuthenticateRequest>(), "127.0.0.1"))
                .Returns(new AuthenticateResponse { JwtToken = "jwt", RefreshToken = "refresh" });

            var result = _controller.Authenticate(new AuthenticateRequest
            {
                Email = "user@example.com",
                Password = "pwd",
            });

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Authenticate_SetsRefreshTokenCookie()
        {
            AttachWithIp(account: null);
            _accountService.Setup(s => s.Authenticate(It.IsAny<AuthenticateRequest>(), It.IsAny<string>()))
                .Returns(new AuthenticateResponse { JwtToken = "jwt", RefreshToken = "refresh-cookie-value" });

            _controller.Authenticate(new AuthenticateRequest { Email = "u", Password = "p" });

            var setCookieHeader = _controller.Response.Headers["Set-Cookie"].ToString();
            setCookieHeader.Should().Contain("refreshToken=refresh-cookie-value");
        }

        [Fact]
        public void Authenticate_BadCredentials_ReturnsBadRequest()
        {
            AttachWithIp(account: null);
            _accountService.Setup(s => s.Authenticate(It.IsAny<AuthenticateRequest>(), It.IsAny<string>()))
                .Throws(new AppException("nope"));

            var result = _controller.Authenticate(new AuthenticateRequest());

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }


        [Fact]
        public void Register_HappyPath_ReturnsOk()
        {
            AttachWithIp(account: null);

            var result = _controller.Register(new RegisterRequest
            {
                UserName = "u",
                Email = "u@example.com",
                Password = "Password!1",
                ConfirmPassword = "Password!1",
                AcceptTerms = true,
            });

            result.Should().BeOfType<OkObjectResult>();
            _accountService.Verify(s => s.Register(It.IsAny<RegisterRequest>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public void Register_ServiceThrows_ReturnsBadRequest()
        {
            AttachWithIp(account: null);
            _accountService.Setup(s => s.Register(It.IsAny<RegisterRequest>(), It.IsAny<string>()))
                .Throws(new AppException("bad"));

            var result = _controller.Register(new RegisterRequest());

            result.Should().BeOfType<BadRequestObjectResult>();
        }


        [Fact]
        public void VerifyEmail_HappyPath_ReturnsOk()
        {
            AttachWithIp(account: null);

            var result = _controller.VerifyEmail(new VerifyEmailRequest { Token = "tok" });

            result.Should().BeOfType<OkObjectResult>();
            _accountService.Verify(s => s.VerifyEmail("tok"), Times.Once);
        }


        [Fact]
        public void ForgotPassword_HappyPath_ReturnsOk()
        {
            AttachWithIp(account: null);

            var result = _controller.ForgotPassword(new ForgotPasswordRequest { Email = "x@y.com" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void ResetPassword_HappyPath_ReturnsOk()
        {
            AttachWithIp(account: null);

            var result = _controller.ResetPassword(new ResetPasswordRequest
            {
                Token = "tok", Password = "p", ConfirmPassword = "p",
            });

            result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public void GetAll_ReturnsOk()
        {
            AttachWithIp(EntityBuilder.NewAccount(role: Role.Admin));
            _accountService.Setup(s => s.GetAll()).Returns(new List<AccountResponse>());

            _controller.GetAll().Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetById_Self_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            AttachWithIp(user);
            _accountService.Setup(s => s.GetById(user.Id.ToString()))
                .Returns(new AccountResponse { Id = user.Id.ToString() });

            var result = _controller.GetById(user.Id.ToString());

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetById_OtherUser_AsNonAdmin_ReturnsUnauthorized()
        {
            var user = EntityBuilder.NewAccount();
            AttachWithIp(user);

            var result = _controller.GetById(Guid.NewGuid().ToString());

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
            _accountService.Verify(s => s.GetById(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetById_OtherUser_AsAdmin_ReturnsOk()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            AttachWithIp(admin);
            var otherId = Guid.NewGuid().ToString();
            _accountService.Setup(s => s.GetById(otherId))
                .Returns(new AccountResponse { Id = otherId });

            var result = _controller.GetById(otherId);

            result.Result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public void Delete_Self_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            AttachWithIp(user);

            var result = _controller.Delete(user.Id.ToString());

            result.Should().BeOfType<OkObjectResult>();
            _accountService.Verify(s => s.Delete(user.Id.ToString()), Times.Once);
        }

        [Fact]
        public void Delete_OtherUser_AsNonAdmin_ReturnsUnauthorized()
        {
            var user = EntityBuilder.NewAccount();
            AttachWithIp(user);

            var result = _controller.Delete(Guid.NewGuid().ToString());

            result.Should().BeOfType<UnauthorizedObjectResult>();
            _accountService.Verify(s => s.Delete(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Delete_OtherUser_AsAdmin_ReturnsOk()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            AttachWithIp(admin);
            var otherId = Guid.NewGuid().ToString();

            var result = _controller.Delete(otherId);

            result.Should().BeOfType<OkObjectResult>();
            _accountService.Verify(s => s.Delete(otherId), Times.Once);
        }


        [Fact]
        public void Search_PassesExcludeIdAsCurrentUser()
        {
            var user = EntityBuilder.NewAccount();
            AttachWithIp(user);
            _accountService.Setup(s => s.SearchByUsername("alice", user.Id.ToString()))
                .Returns(new List<AccountResponse>());

            var result = _controller.Search("alice");

            result.Result.Should().BeOfType<OkObjectResult>();
            _accountService.Verify(s => s.SearchByUsername("alice", user.Id.ToString()), Times.Once);
        }


        [Fact]
        public void RevokeToken_NoCookie_ReturnsBadRequest()
        {
            var user = EntityBuilder.NewAccount();
            AttachWithIp(user);

            var result = _controller.RevokeToken();

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
