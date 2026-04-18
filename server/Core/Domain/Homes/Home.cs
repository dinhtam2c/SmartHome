using Core.Common;
using Core.Primitives;

namespace Core.Domain.Homes;

public class Home : Entity
{
    public Guid Id { get; }

    public string Name { get; private set; }
    public string? Description { get; private set; }

    public long CreatedAt { get; private set; }

    private readonly List<Room> _rooms = [];
    public IReadOnlyCollection<Room> Rooms => _rooms;

    private Home(string name, string? description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        var now = Time.UnixNow();
        CreatedAt = now;
    }

    public static Home Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Home name is required");

        return new(name, description);
    }

    public void Update(string? name, string? description)
    {
        if (name is not null)
            Name = name;
        if (description is not null)
            Description = description;
    }

    public Room AddRoom(string name, string? description)
    {
        var room = new Room(Id, name, description);
        _rooms.Add(room);

        Raise(new RoomAddedDomainEvent(Guid.NewGuid(), Id, room.Id));

        return room;
    }

    public void UpdateRoom(Guid roomId, string? name, string? description)
    {
        var room = _rooms.FirstOrDefault(x => x.Id == roomId)
            ?? throw new InvalidOperationException("Room not found.");

        room.Update(name, description);

        Raise(new RoomUpdatedDomainEvent(Guid.NewGuid(), Id, room.Id));
    }

    public void RemoveRoom(Guid roomId)
    {
        var room = _rooms.FirstOrDefault(l => l.Id == roomId) ??
            throw new InvalidOperationException("Room not found");

        _rooms.Remove(room);

        Raise(new RoomDeletedDomainEvent(Guid.NewGuid(), Id, room.Id));
    }
}
