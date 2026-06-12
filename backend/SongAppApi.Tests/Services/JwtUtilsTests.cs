using FluentAssertions;
using SongAppApi.Authorization;
using SongAppApi.Helpers;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class JwtUtilsTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly JwtUtils _utils;

        public JwtUtilsTests()
        {
            _context = TestDbContextFactory.Create();
            _utils = new JwtUtils(_context, TestSettings.AppSettings());
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public void GenerateJwtToken_ReturnsNonEmptyToken()
        {
            var acc = EntityBuilder.NewAccount();
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            var token = _utils.GenerateJwtToken(acc);

            token.Should().NotBeNullOrEmpty();
            token.Split('.').Should().HaveCount(3); // JWT format de header.payload.signature
        }

        [Fact]
        public void ValidateJwtToken_RoundTrip_ReturnsAccountId()
        {
            var acc = EntityBuilder.NewAccount();
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            var token = _utils.GenerateJwtToken(acc);
            var validated = _utils.ValidateJwtToken(token);

            validated.Should().Be(acc.Id.ToString());
        }

        [Fact]
        public void ValidateJwtToken_NullToken_ReturnsNull()
        {
            _utils.ValidateJwtToken(null!).Should().BeNull();
        }

        [Fact]
        public void ValidateJwtToken_GarbageToken_ReturnsNull()
        {
            _utils.ValidateJwtToken("not-a-real-jwt").Should().BeNull();
        }

        [Fact]
        public void ValidateJwtToken_DifferentSecret_ReturnsNull()
        {
            var acc = EntityBuilder.NewAccount();
            _context.Accounts.Add(acc);
            _context.SaveChanges();

            var token = _utils.GenerateJwtToken(acc);

            // validate with a different secret 
            var otherUtils = new JwtUtils(_context, TestSettings.AppSettings(
                secret: "different-key-different-key-different-key-different-key"));
            otherUtils.ValidateJwtToken(token).Should().BeNull();
        }

        [Fact]
        public void GenerateRefreshToken_ReturnsTokenWithMetadata()
        {
            var token = _utils.GenerateRefreshToken("127.0.0.1");

            token.Token.Should().NotBeNullOrEmpty();
            token.CreatedByIp.Should().Be("127.0.0.1");
            token.Expires.Should().BeAfter(DateTime.UtcNow);
            token.IsActive.Should().BeTrue();
        }

        [Fact]
        public void GenerateRefreshToken_UniqueAcrossCalls()
        {
            var t1 = _utils.GenerateRefreshToken("127.0.0.1");
            var t2 = _utils.GenerateRefreshToken("127.0.0.1");

            t1.Token.Should().NotBe(t2.Token);
        }
    }
}
