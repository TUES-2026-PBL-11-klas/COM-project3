namespace Core.Users;

public sealed class User
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "citizen";
    public int Points { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
