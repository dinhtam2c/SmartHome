using Core.Common;

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

    public Home(string name, string? description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        var now = Time.UnixNow();
        CreatedAt = now;
        UpdatedAt = now;

        Locations = [];
        Gateways = [];
    }

    public void Update(string? name, string? description)
    {
        if (name is not null)
            Name = name;
        if (description is not null)
            Description = description;
        UpdatedAt = Time.UnixNow();
    }
}
