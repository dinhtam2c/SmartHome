using Domain.Common;

namespace Domain.Models.Homes;

public record RoomDeletedDomainEvent(
    Guid Id,
    Guid HomeId,
    Guid RoomId
) : DomainEvent(Id);
