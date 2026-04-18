using Core.Primitives;

namespace Core.Domain.Homes;

public record RoomAddedDomainEvent(
    Guid Id,
    Guid HomeId,
    Guid RoomId
) : DomainEvent(Id);
