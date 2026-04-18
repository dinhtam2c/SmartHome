using Core.Primitives;

namespace Core.Domain.Homes;

public record RoomUpdatedDomainEvent(
    Guid Id,
    Guid HomeId,
    Guid RoomId
) : DomainEvent(Id);
