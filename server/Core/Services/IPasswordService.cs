namespace Core.Services;

public interface IPasswordService
{
    (bool IsValid, string? Error) ValidateComplexity(string password);
    string Hash(string password);
    bool Verify(string password, string hash);
}
