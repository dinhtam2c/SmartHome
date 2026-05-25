using Application.Common.Data;
using Application.Common.Realtime;
using Application.Exceptions;
using Core.Domain.Automations;
using MediatR;

namespace Application.Commands.Automations.DeleteAutomationRule;

public sealed class DeleteAutomationRuleCommandHandler : IRequestHandler<DeleteAutomationRuleCommand>
{
    private readonly IAutomationRuleRepository _automationRuleRepository;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAutomationRuleCommandHandler(
        IAutomationRuleRepository automationRuleRepository,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _automationRuleRepository = automationRuleRepository;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _automationRuleRepository.GetById(request.RuleId, cancellationToken)
            ?? throw new AutomationRuleNotFoundException(request.RuleId);

        if (rule.HomeId != request.HomeId)
            throw new AutomationRuleNotFoundException(request.RuleId);

        _automationRuleRepository.Remove(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtimePublisher.PublishToHome(
            request.HomeId,
            RealtimeDeltaFactory.ForAutomationRuleDeleted(request.HomeId, request.RuleId),
            cancellationToken);
    }
}
