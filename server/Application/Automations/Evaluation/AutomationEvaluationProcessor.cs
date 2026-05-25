using Application.Commands.Automations.ExecuteAutomationRule;
using Application.Common.Data;
using Core.Common;
using Core.Domain.Automations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Automations.Evaluation;

public sealed class AutomationEvaluationProcessor
{
    private readonly IAutomationRuleRepository _automationRuleRepository;
    private readonly IAppReadDbContext _context;
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;

    public AutomationEvaluationProcessor(
        IAutomationRuleRepository automationRuleRepository,
        IAppReadDbContext context,
        ISender sender,
        IUnitOfWork unitOfWork)
    {
        _automationRuleRepository = automationRuleRepository;
        _context = context;
        _sender = sender;
        _unitOfWork = unitOfWork;
    }

    public async Task Process(AutomationEvaluationWorkItem workItem, CancellationToken cancellationToken)
    {
        var triggerDevice = await _context.Devices
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(device => device.Id == workItem.DeviceId, cancellationToken);

        if (triggerDevice?.HomeId is not Guid homeId || !triggerDevice.IsOnline)
            return;

        var rules = await _automationRuleRepository.GetEnabledByConditionTarget(
            homeId,
            workItem.DeviceId,
            workItem.EndpointId,
            workItem.CapabilityId,
            cancellationToken);

        if (rules.Count == 0)
            return;

        foreach (var rule in rules)
        {
            var stateByConditionKey = await LoadConditionStates(rule, homeId, cancellationToken);
            var now = Time.UnixNow();
            var result = AutomationConditionEvaluator.EvaluateRule(rule, stateByConditionKey, now);

            if (!result)
            {
                rule.MarkEvaluated(false, now);
                continue;
            }

            if (rule.LastEvaluationResult == true || !rule.IsCooldownSatisfied(now))
            {
                rule.MarkEvaluated(true, now);
                continue;
            }

            rule.MarkTriggered(now);
            await _sender.Send(
                new ExecuteAutomationRuleCommand(
                    homeId,
                    rule.Id,
                    new AutomationTriggerContext(
                        workItem.DeviceId,
                        workItem.EndpointId,
                        workItem.CapabilityId,
                        workItem.State,
                        "automation")),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, IReadOnlyDictionary<string, object?>>> LoadConditionStates(
        AutomationRule rule,
        Guid homeId,
        CancellationToken cancellationToken)
    {
        var deviceIds = rule.Conditions
            .Select(condition => condition.DeviceId)
            .Distinct()
            .ToList();

        var devices = await _context.Devices
            .AsNoTracking()
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .Where(device => device.HomeId == homeId && deviceIds.Contains(device.Id))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var condition in rule.Conditions)
        {
            var state = devices
                .FirstOrDefault(device => device.Id == condition.DeviceId)
                ?.FindCapability(condition.CapabilityId, condition.EndpointId)
                ?.State;

            if (state is null)
                continue;

            result[AutomationConditionEvaluator.BuildConditionKey(
                condition.DeviceId,
                condition.EndpointId,
                condition.CapabilityId)] = state;
        }

        return result;
    }
}
