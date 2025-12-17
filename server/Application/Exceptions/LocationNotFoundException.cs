namespace Application.Exceptions;

public class LocationNotFoundException : NotFoundException
{
    public LocationNotFoundException(Guid locationId)
        : base($"Location {locationId} not found") { }
}
