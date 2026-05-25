using Domain.Common;

namespace Domain.Models.Homes;

public record RoomAddedDomainEvent(
    Guid Id,
    Guid HomeId,
    Guid RoomId,
    string Name,
    string? Description
) : DomainEvent(Id);
