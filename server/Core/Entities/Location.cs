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

    public Location(Guid id, Guid homeId, string name, string? description, long createdAt)
    {
        Id = id;
        HomeId = homeId;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;

        Devices = [];
    }
}
