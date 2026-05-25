using Application.Common.Data;
using Application.Common.Realtime;
using Application.Exceptions;
using Core.Domain.Automations;
using MediatR;

namespace Application.Commands.Automations.ExecuteAutomationRule;

public sealed class ExecuteAutomationRuleCommandHandler : IRequestHandler<ExecuteAutomationRuleCommand, Guid>
{
    private readonly IAutomationRuleRepository _automationRuleRepository;
    private readonly IAutomationExecutionRepository _automationExecutionRepository;
    private readonly IActionSetProcessor _actionSetProcessor;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public ExecuteAutomationRuleCommandHandler(
        IAutomationRuleRepository automationRuleRepository,
        IAutomationExecutionRepository automationExecutionRepository,
        IActionSetProcessor actionSetProcessor,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _automationRuleRepository = automationRuleRepository;
        _automationExecutionRepository = automationExecutionRepository;
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

        var execution = AutomationExecution.Start(rule, request.Trigger);
        await _automationExecutionRepository.Add(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishAutomationExecutionDelta(execution, cancellationToken);

        await _actionSetProcessor.AdvanceAutomation(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishAutomationExecutionDelta(execution, cancellationToken);

        return execution.Id;
    }

    private Task PublishAutomationExecutionDelta(AutomationExecution execution, CancellationToken cancellationToken)
    {
        return _realtimePublisher.PublishToHome(
            execution.HomeId,
            RealtimeDeltaFactory.ForAutomationExecution(execution),
            cancellationToken);
    }
}
