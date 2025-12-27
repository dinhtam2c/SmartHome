namespace Application.Exceptions;

public class LocationHomeMismatchException : BadRequestException
{
    public LocationHomeMismatchException(Guid locationId, Guid locationHomeId, Guid gatewayHomeId)
        : base($"Location {locationId} belongs to home {locationHomeId}, but device's gateway belongs to home {gatewayHomeId}. Location must belong to the same home as the device's gateway.") { }
}
