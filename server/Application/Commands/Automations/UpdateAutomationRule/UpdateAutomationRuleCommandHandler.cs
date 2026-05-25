using Application.Common.Data;
using Application.Common.Realtime;
using Application.Exceptions;
using Application.Automations.Rules;
using Core.Domain.Automations;
using MediatR;

namespace Application.Commands.Automations.UpdateAutomationRule;

public sealed class UpdateAutomationRuleCommandHandler : IRequestHandler<UpdateAutomationRuleCommand>
{
    private readonly IAutomationRuleRepository _automationRuleRepository;
    private readonly IAppReadDbContext _context;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityStateValidator _capabilityStateValidator;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAutomationRuleCommandHandler(
        IAutomationRuleRepository automationRuleRepository,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityStateValidator capabilityStateValidator,
        ICapabilityCommandValidator capabilityCommandValidator,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _automationRuleRepository = automationRuleRepository;
        _context = context;
        _capabilityRegistry = capabilityRegistry;
        _capabilityStateValidator = capabilityStateValidator;
        _capabilityCommandValidator = capabilityCommandValidator;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _automationRuleRepository.GetById(request.RuleId, cancellationToken)
            ?? throw new AutomationRuleNotFoundException(request.RuleId);

        if (rule.HomeId != request.HomeId)
            throw new AutomationRuleNotFoundException(request.RuleId);

        if (request.CooldownMs.HasValue && request.CooldownMs.Value < 0)
            throw new DomainValidationException("cooldownMs must be >= 0.");

        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Automation name is required.");

        rule.UpdateInfo(
            request.Name,
            request.Description,
            request.IsEnabled,
            request.ConditionLogic,
            request.CooldownMs);

        if (request.Conditions is not null || request.TimeWindow is not null)
        {
            var conditionModels = request.Conditions
                ?? rule.Conditions
                    .OrderBy(condition => condition.Order)
                    .Select(ToConditionModel)
                    .ToList();
            var timeWindowModel = request.TimeWindow
                ?? ToTimeWindowModel(rule);
            var conditions = await AutomationValidationHelper.ValidateAndBuildConditionDefinitions(
                request.HomeId,
                conditionModels,
                _context,
                _capabilityRegistry,
                cancellationToken);
            var timeWindow = AutomationValidationHelper.ValidateAndBuildTimeWindowDefinition(timeWindowModel);

            rule.ReplaceConditions(conditions);
            rule.ReplaceTimeWindow(timeWindow);
        }

        if (request.ActionSet is not null)
        {
            var actionSet = await ActionSetValidationHelper.ValidateAndBuildDefinition(
                request.HomeId,
                request.ActionSet,
                _context,
                _capabilityRegistry,
                _capabilityStateValidator,
                _capabilityCommandValidator,
                cancellationToken);

            rule.ReplaceActionSet(actionSet);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtimePublisher.PublishToHome(
            rule.HomeId,
            RealtimeDeltaFactory.ForAutomationRule(rule, RealtimeChanges.Updated),
            cancellationToken);
    }

    private static AutomationConditionModel ToConditionModel(AutomationCondition condition)
    {
        return new AutomationConditionModel(
            condition.DeviceId,
            condition.EndpointId,
            condition.CapabilityId,
            condition.FieldPath,
            condition.Operator,
            condition.GetCompareValue());
    }

    private static AutomationTimeWindowModel ToTimeWindowModel(AutomationRule rule)
    {
        return !rule.TimeWindowEnabled
            ? new AutomationTimeWindowModel(false, null, null, null)
            : new AutomationTimeWindowModel(
                true,
                FormatMinute(rule.TimeWindowStartMinute),
                FormatMinute(rule.TimeWindowEndMinute),
                DaysFromMask(rule.TimeWindowDaysOfWeekMask));
    }

    private static string? FormatMinute(int? minute)
    {
        if (!minute.HasValue)
            return null;

        var hour = minute.Value / 60;
        var minutes = minute.Value % 60;
        return $"{hour:00}:{minutes:00}";
    }

    private static IReadOnlyList<DayOfWeek> DaysFromMask(int mask)
    {
        var normalizedMask = mask == 0 ? 0b111_1111 : mask;
        return Enum.GetValues<DayOfWeek>()
            .Where(day => (normalizedMask & (1 << (int)day)) != 0)
            .ToList();
    }
}
