using System.Collections.Generic;
using Supermarket.Application.BlackBox.Services;
using Xunit;

namespace Supermarket.UnitTests.BlackBox
{
    public class BlackBoxMetadataSanitizerTests
    {
        [Fact]
        public void Sanitize_RemovesSensitiveKeys_CaseInsensitive()
        {
            var sanitizer = new BlackBoxMetadataSanitizer();
            var metadata = new Dictionary<string, object?>
            {
                ["safe"] = "visible",
                ["password"] = "secret-password",
                ["ApiKey"] = "secret-api-key",
                ["nested"] = new Dictionary<string, object?>
                {
                    ["sessionToken"] = "secret-token",
                    ["note"] = "kept"
                }
            };

            var json = sanitizer.Sanitize(metadata, out var truncated);

            Assert.False(truncated);
            Assert.NotNull(json);
            Assert.Contains("visible", json);
            Assert.Contains("kept", json);
            Assert.DoesNotContain("secret-password", json);
            Assert.DoesNotContain("secret-api-key", json);
            Assert.DoesNotContain("secret-token", json);
            Assert.DoesNotContain("password", json);
            Assert.DoesNotContain("sessionToken", json);
        }

        [Fact]
        public void Sanitize_TruncatesOversizedMetadata()
        {
            var sanitizer = new BlackBoxMetadataSanitizer();
            var metadata = new Dictionary<string, object?>
            {
                ["notes"] = new string('x', 5000)
            };

            var json = sanitizer.Sanitize(metadata, out var truncated);

            Assert.True(truncated);
            Assert.NotNull(json);
            Assert.Contains("metadataTruncated", json);
            Assert.DoesNotContain(new string('x', 100), json);
        }

        [Fact]
        public void Sanitize_ReturnsNull_ForEmptyMetadata()
        {
            var sanitizer = new BlackBoxMetadataSanitizer();

            var json = sanitizer.Sanitize(new Dictionary<string, object?>(), out var truncated);

            Assert.Null(json);
            Assert.False(truncated);
        }
    }
}
