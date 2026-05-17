using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Supermarket.Application.Common.Exports
{
    public interface IPrintHtmlBuilder
    {
        string BuildPrintHtml<T>(IEnumerable<T> data, string title);
    }

    public class PrintHtmlBuilder : IPrintHtmlBuilder
    {
        public string BuildPrintHtml<T>(IEnumerable<T> data, string title)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"<title>{title}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #f2f2f2; }");
            sb.AppendLine("@media print { .no-print { display: none; } }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            sb.AppendLine($"<h2>{title}</h2>");
            sb.AppendLine($"<p>Printed on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

            sb.AppendLine("<table>");
            
            var properties = typeof(T).GetProperties();
            
            // Header
            sb.AppendLine("<thead><tr>");
            foreach (var prop in properties)
            {
                sb.AppendLine($"<th>{prop.Name}</th>");
            }
            sb.AppendLine("</tr></thead>");
            
            // Data
            sb.AppendLine("<tbody>");
            if (data != null)
            {
                foreach (var item in data)
                {
                    sb.AppendLine("<tr>");
                    foreach (var prop in properties)
                    {
                        var val = prop.GetValue(item)?.ToString() ?? "";
                        sb.AppendLine($"<td>{val}</td>");
                    }
                    sb.AppendLine("</tr>");
                }
            }
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
            
            sb.AppendLine("<script>window.onload = function() { window.print(); }</script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
