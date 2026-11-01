namespace PlugAuth.Services;

// Thrown by AuthService.RegisterAsync when an account with the email already exists. Hosts can
// catch this type to answer with a conflict, without catching unrelated InvalidOperationExceptions.
// It derives from InvalidOperationException so code that already catches that type still works.
public class EmailAlreadyRegisteredException : InvalidOperationException
{
    public EmailAlreadyRegisteredException()
        : base("Email is already registered.")
    {
    }

    public EmailAlreadyRegisteredException(Exception innerException)
        : base("Email is already registered.", innerException)
    {
    }
}
