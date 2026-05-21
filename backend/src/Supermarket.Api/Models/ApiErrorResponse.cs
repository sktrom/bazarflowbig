using System.Text.Json.Serialization;

namespace Supermarket.Api.Models
{
    public class ApiErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; } = string.Empty;
    }
}
