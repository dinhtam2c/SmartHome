namespace Application.Common.Errors;

public class HomeNotFoundException : NotFoundException
{
    public HomeNotFoundException(Guid homeId)
        : base($"Home {homeId} not found") { }
}
