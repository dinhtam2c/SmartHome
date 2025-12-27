using Core.Common;

namespace Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsLocked { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    public ICollection<ActionLog> ActionLogs { get; set; }

    public User(string name, string username, string passwordHash, UserRole role,
        string? email, string? phone, bool isLocked)
    {
        Id = Guid.NewGuid();
        Name = name;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        Email = email;
        Phone = phone;
        IsLocked = isLocked;
        var now = Time.UnixNow();
        CreatedAt = now;
        UpdatedAt = now;
        ActionLogs = [];
    }
}

public enum UserRole
{
    Owner,
    Admin,
    Member
}
