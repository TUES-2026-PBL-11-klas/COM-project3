using Core.Services;
using Xunit;

namespace Tests;

public sealed class PasswordServiceTests
{
    private readonly IPasswordService _passwordService = new PasswordService();

    [Theory]
    [InlineData("short")]
    [InlineData("allletters")]
    [InlineData("12345678")]
    public void ValidateComplexity_ReturnsInvalid_ForWeakPasswords(string password)
    {
        var (isValid, error) = _passwordService.ValidateComplexity(password);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateComplexity_ReturnsValid_ForStrongPassword()
    {
        var (isValid, error) = _passwordService.ValidateComplexity("Password1");

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void HashAndVerify_ReturnsTrue_ForCorrectPassword()
    {
        var password = "Password1";
        var hash = _passwordService.Hash(password);

        Assert.True(_passwordService.Verify(password, hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForIncorrectPassword()
    {
        var password = "Password1";
        var hash = _passwordService.Hash(password);

        Assert.False(_passwordService.Verify("Password2", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash()
    {
        Assert.False(_passwordService.Verify("Password1", "invalid-hash"));
    }
}
