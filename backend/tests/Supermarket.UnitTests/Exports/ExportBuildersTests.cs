using System;
using System.Collections.Generic;
using System.Linq;
using Supermarket.Application.Common.Exports;
using Xunit;

namespace Supermarket.UnitTests.Exports
{
    public class ExportFormatBuilderTests
    {
        private class TestData
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        [Fact]
        public void BuildExportFile_ShouldThrow_WhenFormatInvalid()
        {
            var builder = new ExportFormatBuilder();
            var data = new List<TestData>();

            Assert.Throws<ArgumentException>(() => builder.BuildExportFile(data, new List<string>(), "invalid_format"));
        }

        [Fact]
        public void BuildExportFile_ShouldIncludeOnlyVisibleColumns()
        {
            var builder = new ExportFormatBuilder();
            var data = new List<TestData> { new TestData { Id = 1, Name = "Item", Price = 10 } };
            var columns = new List<string> { "Name", "Price" };

            var bytes = builder.BuildExportFile(data, columns, "excel");
            var result = System.Text.Encoding.UTF8.GetString(bytes.Skip(3).ToArray()); // Skip BOM

            Assert.Contains("Name,Price", result);
            Assert.DoesNotContain("Id", result);
        }

        [Fact]
        public void BuildExportFile_ShouldReturnEmptyWithHeader_WhenNoData()
        {
            var builder = new ExportFormatBuilder();
            var data = new List<TestData>();
            var columns = new List<string> { "Id" };

            var bytes = builder.BuildExportFile(data, columns, "excel");
            var result = System.Text.Encoding.UTF8.GetString(bytes.Skip(3).ToArray());

            Assert.Contains("Id", result);
            // No data lines
        }
    }

    public class PrintHtmlBuilderTests
    {
        private class TestData
        {
            public string Title { get; set; } = string.Empty;
        }

        [Fact]
        public void BuildPrintHtml_ShouldIncludeTableData_And_ExcludeChartData()
        {
            var builder = new PrintHtmlBuilder();
            var data = new List<TestData> { new TestData { Title = "Test Title" } };

            var html = builder.BuildPrintHtml(data, "Report Title");

            Assert.Contains("<title>Report Title</title>", html);
            Assert.Contains("Test Title", html);
            Assert.DoesNotContain("Chart", html); // By design PrintHtmlBuilder doesn't handle charts
        }
    }
}
