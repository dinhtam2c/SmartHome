namespace Application.Exceptions;

public class HomeNotFoundException : NotFoundException
{
    public HomeNotFoundException(Guid homeId)
        : base($"Home {homeId} not found") { }
}
