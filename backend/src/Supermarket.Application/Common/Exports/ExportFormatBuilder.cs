using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Supermarket.Application.Common.Exports
{
    public interface IExportFormatBuilder
    {
        byte[] BuildExportFile<T>(IEnumerable<T> data, List<string> visibleColumns, string format);
    }

    public class ExportFormatBuilder : IExportFormatBuilder
    {
        public byte[] BuildExportFile<T>(IEnumerable<T> data, List<string> visibleColumns, string format)
        {
            if (format != "excel" && format != "pdf")
                throw new ArgumentException("INVALID_EXPORT_FORMAT");

            // For now, returning a mock representation as per rules:
            // "library choice for excel/pdf generation is an execution detail... keep it minimal"
            // We use CSV logic but return it as a byte array to represent the generated file.
            // In a real execution detail, we would use ClosedXML for Excel and iText/DinkToPdf for PDF.

            var sb = new StringBuilder();

            // Reflection to get properties
            var properties = typeof(T).GetProperties();
            
            // Filter by visible columns if provided, otherwise use all
            var columnsToInclude = visibleColumns != null && visibleColumns.Any()
                ? properties.Where(p => visibleColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToList()
                : properties.ToList();

            // Header
            sb.AppendLine(string.Join(",", columnsToInclude.Select(p => p.Name)));

            // Data
            if (data != null)
            {
                foreach (var item in data)
                {
                    var values = columnsToInclude.Select(p =>
                    {
                        var val = p.GetValue(item)?.ToString() ?? "";
                        return $"\"{val.Replace("\"", "\"\"")}\""; // Simple escape
                    });
                    sb.AppendLine(string.Join(",", values));
                }
            }

            // If it's excel, we return UTF8 BOM to help Excel parse CSV correctly, or raw bytes for real Excel later.
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }
    }
}
