using Application.Ports.Persistence;
using Domain.Models.ActionSets;
using Domain.Models.Devices;
using Microsoft.EntityFrameworkCore;

namespace Application.BusinessServices.ActionSets.Execution;

public sealed class ActionSetProcessor : IActionSetProcessor
{
    private enum AdvanceResult
    {
        Done,
        WaitingForResult,
        Failed
    }

    private readonly IAppReadDbContext _context;
    private readonly IActionDispatcher _dispatcher;

    public ActionSetProcessor(
        IAppReadDbContext context,
        IActionDispatcher dispatcher)
    {
        _context = context;
        _dispatcher = dispatcher;
    }

    public async Task Advance(ActionSetExecution execution, CancellationToken cancellationToken)
    {
        if (execution.Status != ActionSetExecutionStatus.Running)
            return;

        var devices = await LoadDevices(
            execution.HomeId,
            execution.Actions.Select(action => action.DeviceId).Distinct().ToList(),
            cancellationToken);
        var autoFixedPrerequisites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (execution.Status == ActionSetExecutionStatus.Running)
        {
            switch (execution.Phase)
            {
                case ActionExecutionPhase.BeforeHooks:
                    {
                        var result = await RunSectionSequential(
                            execution,
                            ActionSetSection.Before,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForResult)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            EnterFailureBranch(execution, "Skipped because a before hook failed.");
                            continue;
                        }

                        execution.EnterPhase(ActionExecutionPhase.MainActions);
                        continue;
                    }
                case ActionExecutionPhase.MainActions:
                    {
                        var result = execution.ExecutionMode == ActionExecutionMode.Parallel
                            ? await RunMainParallel(execution, devices, autoFixedPrerequisites, cancellationToken)
                            : await RunMainSequential(execution, devices, autoFixedPrerequisites, cancellationToken);

                        if (result == AdvanceResult.WaitingForResult)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            EnterFailureBranch(execution, "Skipped because main actions failed.");
                            continue;
                        }

                        execution.SkipPendingActions(
                            ActionSetSection.OnFailure,
                            "Skipped because main actions succeeded.");
                        execution.EnterPhase(ActionExecutionPhase.OnSuccessHooks);
                        continue;
                    }
                case ActionExecutionPhase.OnSuccessHooks:
                    {
                        var result = await RunSectionSequential(
                            execution,
                            ActionSetSection.OnSuccess,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForResult)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            execution.SkipPendingActions(
                                ActionSetSection.OnSuccess,
                                "Skipped because a success hook failed.");
                        }

                        execution.SkipPendingActions(
                            ActionSetSection.OnFailure,
                            "Skipped because success hooks completed.");
                        execution.Complete();
                        return;
                    }
                case ActionExecutionPhase.OnFailureHooks:
                    {
                        var result = await RunSectionSequential(
                            execution,
                            ActionSetSection.OnFailure,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForResult)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            execution.SkipPendingActions(
                                ActionSetSection.OnFailure,
                                "Skipped because a failure hook failed.");
                        }

                        execution.Complete();
                        return;
                    }
                case ActionExecutionPhase.Completed:
                    return;
                default:
                    execution.Complete();
                    return;
            }
        }
    }

    private async Task<AdvanceResult> RunSectionSequential(
        ActionSetExecution execution,
        ActionSetSection section,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActionWaitingForResult(section))
            return AdvanceResult.WaitingForResult;

        if (execution.HasFailedAction(section))
            return AdvanceResult.Failed;

        foreach (var action in execution.FindPendingActions(section))
        {
            await _dispatcher.Dispatch(execution, action, devices, autoFixedPrerequisites, cancellationToken);

            if (action.Status == ActionExecutionStatus.WaitingForResult)
                return AdvanceResult.WaitingForResult;

            if (action.Status == ActionExecutionStatus.Failed)
                return AdvanceResult.Failed;
        }

        return AdvanceResult.Done;
    }

    private async Task<AdvanceResult> RunMainSequential(
        ActionSetExecution execution,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActionWaitingForResult(ActionSetSection.Main))
            return AdvanceResult.WaitingForResult;

        if (!execution.ContinueOnError && execution.HasFailedAction(ActionSetSection.Main))
            return AdvanceResult.Failed;

        foreach (var action in execution.FindPendingActions(ActionSetSection.Main))
        {
            await _dispatcher.Dispatch(execution, action, devices, autoFixedPrerequisites, cancellationToken);

            if (action.Status == ActionExecutionStatus.WaitingForResult)
                return AdvanceResult.WaitingForResult;

            if (action.Status == ActionExecutionStatus.Failed && !execution.ContinueOnError)
            {
                execution.SkipPendingActions(
                    ActionSetSection.Main,
                    "Skipped because a previous main action failed.");
                return AdvanceResult.Failed;
            }
        }

        return execution.HasFailedAction(ActionSetSection.Main)
            ? AdvanceResult.Failed
            : AdvanceResult.Done;
    }

    private async Task<AdvanceResult> RunMainParallel(
        ActionSetExecution execution,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActionWaitingForResult(ActionSetSection.Main))
            return AdvanceResult.WaitingForResult;

        foreach (var action in execution.FindPendingActions(ActionSetSection.Main))
        {
            await _dispatcher.Dispatch(execution, action, devices, autoFixedPrerequisites, cancellationToken);
        }

        if (execution.HasActionWaitingForResult(ActionSetSection.Main))
            return AdvanceResult.WaitingForResult;

        return execution.HasFailedAction(ActionSetSection.Main)
            ? AdvanceResult.Failed
            : AdvanceResult.Done;
    }

    private async Task<Dictionary<Guid, Device>> LoadDevices(
        Guid homeId,
        IReadOnlyCollection<Guid> deviceIds,
        CancellationToken cancellationToken)
    {
        if (deviceIds.Count == 0)
            return [];

        return await _context.Devices
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .Where(device => device.HomeId == homeId && deviceIds.Contains(device.Id))
            .ToDictionaryAsync(device => device.Id, cancellationToken);
    }

    private static void EnterFailureBranch(ActionSetExecution execution, string reason)
    {
        execution.SkipPendingActions(ActionSetSection.Before, reason);
        execution.SkipPendingActions(ActionSetSection.Main, reason);
        execution.SkipPendingActions(ActionSetSection.OnSuccess, reason);
        execution.EnterPhase(ActionExecutionPhase.OnFailureHooks);
    }

}
