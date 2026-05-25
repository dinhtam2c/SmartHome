using Core.Primitives;

namespace Core.Domain.Homes;

public record RoomDeletedDomainEvent(
    Guid Id,
    Guid HomeId,
    Guid RoomId
) : DomainEvent(Id);
