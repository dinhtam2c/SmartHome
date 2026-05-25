using Domain.Models.ActionSets;
using Domain.Models.Devices;

namespace Application.BusinessServices.ActionSets.Execution;

public interface IActionDispatcher
{
    Task Dispatch(
        ActionSetExecution execution,
        ActionSetActionExecution action,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken);
}
