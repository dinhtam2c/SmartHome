using MediatR;

namespace Core.Primitives;

public abstract record DomainEvent(Guid Id) : INotification;
