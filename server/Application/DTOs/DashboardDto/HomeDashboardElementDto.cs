using Core.Entities;

namespace Application.DTOs.DashboardDto;

public record HomeDashboardElementDto
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }

    public HomeDashboardElementDto(Home home)
    {
        Id = home.Id;
        Name = home.Name;
        Description = home.Description;
    }
}
