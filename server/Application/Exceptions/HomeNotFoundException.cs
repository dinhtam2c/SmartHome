namespace Application.Exceptions;

public class HomeNotFoundException : NotFoundException
{
    public HomeNotFoundException(Guid homeId)
        : base($"Device {homeId} not found") { }
}
