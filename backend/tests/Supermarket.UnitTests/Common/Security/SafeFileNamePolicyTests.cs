using System;
using Supermarket.Application.Common.Security;
using Xunit;

namespace Supermarket.UnitTests.Common.Security
{
    public class SafeFileNamePolicyTests
    {
        [Theory]
        [InlineData("safe_name.xlsx", true)]
        [InlineData("report-2024.pdf", true)]
        [InlineData("../evil.xlsx", false)]
        [InlineData("..\\evil.xlsx", false)]
        [InlineData("folder/file.xlsx", false)]
        [InlineData("C:\\temp\\evil.xlsx", false)]
        [InlineData("\\\\server\\share\\evil.xlsx", false)]
        [InlineData("file:name.xlsx", false)]
        [InlineData("file\0name.xlsx", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSafe_ReturnsExpectedResult(string? fileName, bool expected)
        {
            Assert.Equal(expected, SafeFileNamePolicy.IsSafe(fileName));
        }

        [Fact]
        public void GetSafeFileName_SafeName_ReturnsSameName()
        {
            var result = SafeFileNamePolicy.GetSafeFileName("safe_name.xlsx", "fallback.xlsx");
            Assert.Equal("safe_name.xlsx", result);
        }

        [Fact]
        public void GetSafeFileName_UnsafeName_ReturnsFallbackSanitized()
        {
            var result = SafeFileNamePolicy.GetSafeFileName("../evil.xlsx", "fallback_report.xlsx");
            Assert.Equal("fallback_report.xlsx", result);
        }

        [Fact]
        public void GetSafeFileName_UnsafeFallback_ThrowsOrSanitizes()
        {
            var result = SafeFileNamePolicy.GetSafeFileName("../evil.xlsx", "folder/fallback.xlsx");
            // should sanitize the fallback
            Assert.Equal("folder_fallback.xlsx", result);
        }

        [Fact]
        public void GetSafeFileName_TruncatesLongNames()
        {
            var longName = new string('A', 150) + ".xlsx";
            var result = SafeFileNamePolicy.GetSafeFileName(longName, "fallback.xlsx");
            
            Assert.True(result.Length <= SafeFileNamePolicy.MaxFileNameLength);
            Assert.EndsWith(".xlsx", result);
            Assert.Equal(new string('A', SafeFileNamePolicy.MaxFileNameLength - 5) + ".xlsx", result);
        }

        [Theory]
        [InlineData("excel", ".xlsx")]
        [InlineData("EXCEL", ".xlsx")]
        [InlineData("pdf", ".pdf")]
        [InlineData("PDF", ".pdf")]
        public void ValidateAndGetFormatExtension_ValidFormat_ReturnsExpected(string format, string expected)
        {
            Assert.Equal(expected, SafeFileNamePolicy.ValidateAndGetFormatExtension(format));
        }

        [Fact]
        public void ValidateAndGetFormatExtension_InvalidFormat_ThrowsException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => SafeFileNamePolicy.ValidateAndGetFormatExtension("exe"));
            Assert.Equal("INVALID_EXPORT_FORMAT", ex.Message);
        }
    }
}
