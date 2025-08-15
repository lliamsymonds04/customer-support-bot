namespace SupportBot.Models;

public enum Roles
{
    User,
    Admin
}

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Roles Role { get; set; } = Roles.User; // Default role is 'User'
}
