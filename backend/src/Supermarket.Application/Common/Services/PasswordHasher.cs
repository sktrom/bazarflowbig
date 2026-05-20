using System.Security.Cryptography;
using System.Text;
using Supermarket.Application.Common.Interfaces;
using IdentityPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<object>;
using IdentityPasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace Supermarket.Application.Common.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private static readonly object IdentityUser = new();
        private readonly IdentityPasswordHasher _identityHasher = new();

        public string Hash(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            return _identityHasher.HashPassword(IdentityUser, plainText);
        }

        public PasswordVerifyResult Verify(string plainText, string storedHash)
        {
            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(storedHash))
                return PasswordVerifyResult.Invalid;

            try
            {
                var identityResult = _identityHasher.VerifyHashedPassword(IdentityUser, storedHash, plainText);
                if (identityResult == IdentityPasswordVerificationResult.Success)
                    return PasswordVerifyResult.Valid;

                if (identityResult == IdentityPasswordVerificationResult.SuccessRehashNeeded)
                    return PasswordVerifyResult.ValidNeedsRehash;
            }
            catch (FormatException)
            {
                // Non-Identity stored values may be legacy SHA256 or invalid plaintext.
            }

            if (IsLegacySha256(storedHash) &&
                string.Equals(ComputeLegacySha256(plainText), storedHash, StringComparison.OrdinalIgnoreCase))
            {
                return PasswordVerifyResult.ValidNeedsRehash;
            }

            return PasswordVerifyResult.Invalid;
        }

        private static string ComputeLegacySha256(string plainText)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var computedHashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(computedHashBytes).ToLowerInvariant();
        }

        private static bool IsLegacySha256(string storedHash)
        {
            if (storedHash.Length != 64)
                return false;

            return storedHash.All(IsHexChar);
        }

        private static bool IsHexChar(char c)
        {
            return c is >= '0' and <= '9'
                or >= 'a' and <= 'f'
                or >= 'A' and <= 'F';
        }
    }
}
