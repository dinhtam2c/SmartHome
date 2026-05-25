using Domain.Common;

namespace Domain.Models.Homes;

public record RoomUpdatedDomainEvent(
    Guid Id,
    Guid HomeId,
    Guid RoomId,
    string Name,
    string? Description
) : DomainEvent(Id);
