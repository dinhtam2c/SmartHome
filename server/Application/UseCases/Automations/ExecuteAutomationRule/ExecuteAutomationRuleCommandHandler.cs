using Application.BusinessServices.ActionSets.Execution;
using Application.BusinessServices.Automations.Realtime;
using Application.Common.Errors;
using Application.Ports.Persistence;
using Application.Ports.Realtime;
using Domain.Models.ActionSets;
using Domain.Models.Automations;
using MediatR;

namespace Application.UseCases.Automations.ExecuteAutomationRule;

public sealed class ExecuteAutomationRuleCommandHandler : IRequestHandler<ExecuteAutomationRuleCommand, Guid>
{
    private readonly IAutomationRuleRepository _automationRuleRepository;
    private readonly IActionSetExecutionRepository _executionRepository;
    private readonly IActionSetProcessor _actionSetProcessor;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public ExecuteAutomationRuleCommandHandler(
        IAutomationRuleRepository automationRuleRepository,
        IActionSetExecutionRepository executionRepository,
        IActionSetProcessor actionSetProcessor,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _automationRuleRepository = automationRuleRepository;
        _executionRepository = executionRepository;
        _actionSetProcessor = actionSetProcessor;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(ExecuteAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _automationRuleRepository.GetById(request.RuleId, cancellationToken)
            ?? throw new AutomationRuleNotFoundException(request.RuleId);

        if (rule.HomeId != request.HomeId)
            throw new AutomationRuleNotFoundException(request.RuleId);

        if (!rule.IsEnabled)
            throw new DomainValidationException($"Automation rule '{rule.Id}' is disabled.");

        var execution = ActionSetExecution.Start(
            ActionSetExecutionSource.Automation,
            rule.Id,
            rule.ActionSet.Id,
            rule.HomeId,
            rule.ActionSet.ExecutionMode,
            rule.ActionSet.ContinueOnError,
            rule.ActionSet.Actions);
        await _executionRepository.Add(execution, cancellationToken);

        await _actionSetProcessor.Advance(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishAutomationExecutionDelta(execution, cancellationToken);

        return execution.Id;
    }

    private Task PublishAutomationExecutionDelta(ActionSetExecution execution, CancellationToken cancellationToken)
    {
        return _realtimePublisher.PublishToHome(
            execution.HomeId,
            AutomationRealtime.ForExecution(execution),
            cancellationToken);
    }
}
