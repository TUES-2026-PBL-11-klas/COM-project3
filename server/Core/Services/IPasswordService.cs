namespace Core.Services;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);

    /// <summary>
    /// Validates password complexity: at least 8 chars, one letter, one digit.
    /// </summary>
    (bool IsValid, string? Error) ValidateComplexity(string password);
}
