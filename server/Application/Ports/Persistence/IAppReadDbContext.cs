using Domain.Models.Automations;
using Domain.Models.Devices;
using Domain.Models.Floors;
using Domain.Models.Homes;
using Domain.Models.Scenes;

namespace Application.Ports.Persistence;

public interface IAppReadDbContext
{
    IQueryable<Device> Devices { get; }
    IQueryable<AutomationRule> AutomationRules { get; }
    IQueryable<Floor> Floors { get; }
    IQueryable<Home> Homes { get; }
    IQueryable<Scene> Scenes { get; }
}
