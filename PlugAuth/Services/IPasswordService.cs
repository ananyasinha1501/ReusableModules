using PlugAuth.Entities;

namespace PlugAuth.Services;

public interface IPasswordService
{
    string HashPassword(AuthUser user, string password);

    bool VerifyPassword(
        AuthUser user,
        string password,
        string passwordHash);
}