namespace Supermarket.Contracts.Auth
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DeviceCode { get; set; } = string.Empty;
    }
}
