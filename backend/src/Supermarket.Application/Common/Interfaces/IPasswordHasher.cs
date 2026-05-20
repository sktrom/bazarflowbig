namespace Supermarket.Application.Common.Interfaces
{
    public enum PasswordVerifyResult
    {
        Invalid = 0,
        Valid = 1,
        ValidNeedsRehash = 2
    }

    public interface IPasswordHasher
    {
        string Hash(string plainText);
        PasswordVerifyResult Verify(string plainText, string storedHash);
    }
}
