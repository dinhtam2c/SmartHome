using Domain.Common;

namespace Domain.Models.Homes;

public class Room
{
    public Guid Id { get; private set; }
    public Guid HomeId { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public long CreatedAt { get; private set; }

    private Room()
    {
    }

    internal Room(Guid homeId, string name, string? description)
    {
        Id = Guid.NewGuid();
        HomeId = homeId;
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        var now = UnixTime.Now();
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
