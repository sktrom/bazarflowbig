using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Common.Services;
using Xunit;

namespace Supermarket.UnitTests.Common
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void Hash_ShouldNotEqualPlainText()
        {
            var hash = _hasher.Hash("strong-password");

            Assert.NotEqual("strong-password", hash);
            Assert.False(string.IsNullOrWhiteSpace(hash));
        }

        [Fact]
        public void Verify_NewHash_ShouldReturnValid()
        {
            var hash = _hasher.Hash("strong-password");

            var result = _hasher.Verify("strong-password", hash);

            Assert.Equal(PasswordVerifyResult.Valid, result);
        }

        [Fact]
        public void Verify_WrongPassword_ShouldReturnInvalid()
        {
            var hash = _hasher.Hash("strong-password");

            var result = _hasher.Verify("wrong-password", hash);

            Assert.Equal(PasswordVerifyResult.Invalid, result);
        }

        [Fact]
        public void Verify_LegacySha256_ShouldReturnValidNeedsRehash()
        {
            const string legacyHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9";

            var result = _hasher.Verify("admin123", legacyHash);

            Assert.Equal(PasswordVerifyResult.ValidNeedsRehash, result);
        }

        [Fact]
        public void Verify_PlainTextStoredPassword_ShouldReturnInvalid()
        {
            var result = _hasher.Verify("plain-password", "plain-password");

            Assert.Equal(PasswordVerifyResult.Invalid, result);
        }
    }
}
