using Core.Domain.Automations;
using Core.Domain.Scenes;

namespace Application.ActionSets;

public interface IActionSetProcessor
{
    Task AdvanceScene(SceneExecution execution, CancellationToken cancellationToken);
    Task AdvanceAutomation(AutomationExecution execution, CancellationToken cancellationToken);
}
