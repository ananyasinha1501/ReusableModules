using Microsoft.Extensions.Options;
using PlugAuth.Configuration;
using PlugAuth.DTOs;
using PlugAuth.Entities;
using PlugAuth.Services;
using PlugAuth.Stores;
using Xunit;

namespace PlugAuth.Tests;

// AuthService.RegisterAsync: a duplicate email is reported with EmailAlreadyRegisteredException,
// validation failures keep throwing ArgumentException, and failures from the store are passed on
// unchanged rather than being mistaken for a duplicate.
public class RegisterAsyncTests
{
    private const string ExistingEmail = "taken@test.local";

    [Fact]
    public async Task NewEmail_CreatesUser_AndReturnsTokens()
    {
        var store = new FakeStore();

        var response = await CreateService(store).RegisterAsync(Request("new@test.local"));

        Assert.Equal("new@test.local", response.Email);
        Assert.False(string.IsNullOrEmpty(response.AccessToken));
        Assert.Single(store.Created);
    }

    [Theory]
    [InlineData(ExistingEmail)]
    [InlineData("  TAKEN@test.LOCAL  ")] // the same normalization as the stored email
    public async Task ExistingEmail_ThrowsEmailAlreadyRegistered_AndCreatesNothing(string email)
    {
        var store = new FakeStore();

        var error = await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(() => CreateService(store).RegisterAsync(Request(email)));

        Assert.IsAssignableFrom<InvalidOperationException>(error); // existing catch blocks keep working
        Assert.Equal("Email is already registered.", error.Message);
        Assert.Empty(store.Created);
    }

    [Theory]
    [InlineData("", "Password-1", UserRole.Buyer, "Email is required.")]
    [InlineData("   ", "Password-1", UserRole.Buyer, "Email is required.")]
    [InlineData("a@test.local", "", UserRole.Buyer, "Password is required.")]
    [InlineData("a@test.local", "Password-1", UserRole.Admin, "The requested role cannot be self-registered.")]
    [InlineData("a@test.local", "Password-1", (UserRole)99, "The requested role cannot be self-registered.")]
    public async Task ValidationFailures_StillThrowArgumentException(string email, string password, UserRole role, string message)
    {
        var store = new FakeStore();

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(store).RegisterAsync(new RegisterRequest { Email = email, Password = password, Role = role }));

        Assert.Equal(message, error.Message);
        Assert.Empty(store.Created);
    }

    [Fact]
    public async Task EmailLongerThanTheLimit_IsAValidationFailure_AtTheLimitIsAccepted()
    {
        var domain = "@test.local";
        var atLimit = new string('a', AuthService.MaxEmailLength - domain.Length) + domain;
        var store = new FakeStore();

        var error = await Assert.ThrowsAsync<ArgumentException>(() => CreateService(store).RegisterAsync(Request("a" + atLimit)));
        Assert.Equal("Email must be at most 256 characters.", error.Message);
        Assert.Empty(store.Created);

        await CreateService(store).RegisterAsync(Request(atLimit));
        Assert.Single(store.Created);
    }

    [Fact]
    public async Task ValidationRunsBeforeTheDuplicateCheck()
    {
        // An Admin request for a taken email is still a validation failure, not a duplicate.
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(new FakeStore()).RegisterAsync(new RegisterRequest { Email = ExistingEmail, Password = "Password-1", Role = UserRole.Admin }));
    }

    [Fact]
    public async Task UnrelatedInvalidOperationFromEmailCheck_IsNotTurnedIntoDuplicate()
    {
        var failure = new InvalidOperationException("A second operation was started on this context instance.");
        var store = new FakeStore { EmailExistsFailure = failure };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(store).RegisterAsync(Request("new@test.local")));

        Assert.Same(failure, error);
        Assert.IsNotType<EmailAlreadyRegisteredException>(error);
    }

    [Fact]
    public async Task UnrelatedInvalidOperationFromCreate_IsNotTurnedIntoDuplicate()
    {
        var failure = new InvalidOperationException("An exception has been raised that is likely due to a transient failure.");
        var store = new FakeStore { CreateFailure = failure };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(store).RegisterAsync(Request("new@test.local")));

        Assert.Same(failure, error);
    }

    [Fact]
    public async Task StoreReportingADuplicateDuringCreate_IsPassedOnAsDuplicate()
    {
        // A store that detects a concurrent duplicate (e.g. a unique index) reports it with the same type.
        var store = new FakeStore { CreateFailure = new EmailAlreadyRegisteredException(new Exception("unique index")) };

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(() => CreateService(store).RegisterAsync(Request("new@test.local")));
    }

    private static RegisterRequest Request(string email) => new() { Email = email, Password = "Password-1", Role = UserRole.Buyer };

    private static AuthService CreateService(IAuthUserStore store) =>
        new(store, new PasswordService(), new TokenService(Options.Create(Jwt)), Options.Create(Jwt));

    private static readonly JwtOptions Jwt = new()
    {
        Issuer = "PlugAuth.Tests",
        Audience = "PlugAuth.Tests",
        Key = "plugauth-test-only-signing-key-0123456789-abcdef"
    };

    private sealed class FakeStore : IAuthUserStore
    {
        public List<AuthUser> Created { get; } = new();
        public Exception? EmailExistsFailure { get; init; }
        public Exception? CreateFailure { get; init; }

        public Task<bool> EmailExistsAsync(string email) =>
            EmailExistsFailure != null ? Task.FromException<bool>(EmailExistsFailure) : Task.FromResult(email == ExistingEmail);

        public Task CreateUserAsync(AuthUser user)
        {
            if (CreateFailure != null) return Task.FromException(CreateFailure);
            Created.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveRefreshTokenAsync(RefreshToken refreshToken) => Task.CompletedTask;
        public Task<AuthUser?> FindByEmailAsync(string email) => Task.FromResult<AuthUser?>(null);
        public Task<AuthUser?> FindByIdAsync(Guid userId) => Task.FromResult<AuthUser?>(null);
        public Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash) => Task.FromResult<RefreshToken?>(null);
        public Task UpdateRefreshTokenAsync(RefreshToken refreshToken) => Task.CompletedTask;
    }
}
