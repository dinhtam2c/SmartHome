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
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        var now = Time.UnixNow();
        CreatedAt = now;
    }

    internal void Update(string? name, string? description)
    {
        if (name is not null)
            Name = NormalizeName(name);

        Description = NormalizeDescription(description);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name is required.", nameof(name));

        return name.Trim();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
