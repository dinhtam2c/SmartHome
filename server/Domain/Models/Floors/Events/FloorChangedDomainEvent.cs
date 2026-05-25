using Domain.Common;

namespace Domain.Models.Floors;

public sealed record FloorChangedDomainEvent(
    Guid Id,
    Guid FloorId,
    Guid HomeId,
    string Reason
) : DomainEvent(Id);
