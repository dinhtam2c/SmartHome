namespace Core.Entities;

public class Home
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    public ICollection<Location> Locations { get; set; }
    public ICollection<Gateway> Gateways { get; set; }

    public Home(Guid id, string name, string? description, long createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;

        Locations = [];
        Gateways = [];
    }
}
