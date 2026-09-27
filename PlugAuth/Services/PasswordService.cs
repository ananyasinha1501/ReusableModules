using Microsoft.AspNetCore.Identity;
using PlugAuth.Entities;

namespace PlugAuth.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<AuthUser> _passwordHasher = new();

    public string HashPassword(AuthUser user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(
        AuthUser user,
        string password,
        string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            passwordHash,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}