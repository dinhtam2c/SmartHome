using Core.Common;

namespace Core.Entities;

public class Location
{
    public Guid Id { get; set; }
    public Guid HomeId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    public Home? Home { get; set; }
    public ICollection<Device> Devices { get; set; }

    public Location(Guid homeId, string name, string? description)
    {
        Id = Guid.NewGuid();
        HomeId = homeId;
        Name = name;
        Description = description;
        var now = Time.UnixNow();
        CreatedAt = now;
        UpdatedAt = now;

        Devices = [];
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
