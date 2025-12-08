namespace Application.Exceptions;

public class ActuatorNotFoundException : NotFoundException
{
    public ActuatorNotFoundException(Guid actuatorId)
        : base($"Actuator {actuatorId} not found") { }
}
