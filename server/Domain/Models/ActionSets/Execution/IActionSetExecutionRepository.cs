namespace Domain.Models.ActionSets;

public interface IActionSetExecutionRepository
{
    Task Add(ActionSetExecution execution, CancellationToken cancellationToken = default);

    Task<ActionSetExecution?> GetByDeviceCommandExecutionId(
        Guid deviceCommandExecutionId,
        CancellationToken cancellationToken = default);
}
