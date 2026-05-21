namespace Supermarket.Application.Auth.Interfaces
{
    public interface IAuthPolicy
    {
        bool AllowDefaultDeviceLogin { get; }
    }
}
