namespace Supermarket.Contracts.Auth
{
    public class LogoutResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
