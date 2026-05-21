using Microsoft.Extensions.Hosting;
using Supermarket.Application.Auth.Interfaces;

namespace Supermarket.Api.Services
{
    public class AuthPolicy : IAuthPolicy
    {
        private readonly IHostEnvironment _environment;

        public AuthPolicy(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public bool AllowDefaultDeviceLogin => _environment.IsDevelopment();
    }
}
