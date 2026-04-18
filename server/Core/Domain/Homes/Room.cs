using Core.Common;

namespace Core.Domain.Homes;

public class Room
{
    public Guid Id { get; }
    public Guid HomeId { get; }

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public long CreatedAt { get; private set; }

    internal Room(Guid homeId, string name, string? description)
    {
        Id = Guid.NewGuid();
        HomeId = homeId;
        Name = name;
        Description = description;
        var now = Time.UnixNow();
        CreatedAt = now;
    }

    internal void Update(string? name, string? description)
    {
        if (name is not null)
            Name = name;
        if (description is not null)
            Description = description;
    }
}
