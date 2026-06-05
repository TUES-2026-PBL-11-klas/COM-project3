namespace Core.Services;

public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public (bool IsValid, string? Error) ValidateComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password is required.");

        if (password.Length < 8)
            return (false, "Password must be at least 8 characters long.");

        if (!password.Any(char.IsLetter))
            return (false, "Password must contain at least one letter.");

        if (!password.Any(char.IsDigit))
            return (false, "Password must contain at least one digit.");

        return (true, null);
    }
}
