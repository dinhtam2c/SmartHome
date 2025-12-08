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

    public User(Guid id, string name, string username, string passwordHash, UserRole role,
        string? email, string? phone, bool isLocked, long createdAt, long updatedAt)
    {
        Id = id;
        Name = name;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        Email = email;
        Phone = phone;
        IsLocked = isLocked;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        ActionLogs = [];
    }
}

public enum UserRole
{
    Owner,
    Admin,
    Member
}
