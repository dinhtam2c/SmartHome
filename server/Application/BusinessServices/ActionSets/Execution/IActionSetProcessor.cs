using Domain.Models.ActionSets;

namespace Application.BusinessServices.ActionSets.Execution;

public interface IActionSetProcessor
{
    Task Advance(ActionSetExecution execution, CancellationToken cancellationToken);
}
