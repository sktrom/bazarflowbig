using System;
using System.Security.Cryptography;
using Supermarket.Application.Auth.Interfaces;

namespace Supermarket.Application.Auth.Services
{
    public class SessionTokenGenerator : ISessionTokenGenerator
    {
        public string Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
