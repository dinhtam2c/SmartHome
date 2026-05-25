using Application.BusinessServices.Automations.Rules;
using Application.BusinessServices.Capabilities.Validation;
using Application.Ports.Registries;
using Application.BusinessServices.ActionSets.Validation;
using Application.BusinessServices.Automations.Realtime;
using Application.Ports.Persistence;
using Application.Common.Realtime;
using Application.Ports.Realtime;
using Application.Common.Errors;
using Domain.Models.Automations;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Automations.CreateAutomationRule;

public sealed class CreateAutomationRuleCommandHandler : IRequestHandler<CreateAutomationRuleCommand, Guid>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IAutomationRuleRepository _automationRuleRepository;
    private readonly IAppReadDbContext _context;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityStateValidator _capabilityStateValidator;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAutomationRuleCommandHandler(
        IHomeRepository homeRepository,
        IAutomationRuleRepository automationRuleRepository,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityStateValidator capabilityStateValidator,
        ICapabilityCommandValidator capabilityCommandValidator,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _automationRuleRepository = automationRuleRepository;
        _context = context;
        _capabilityRegistry = capabilityRegistry;
        _capabilityStateValidator = capabilityStateValidator;
        _capabilityCommandValidator = capabilityCommandValidator;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        _ = await _homeRepository.GetById(request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Automation name is required.");

        if (request.CooldownMs < 0)
            throw new DomainValidationException("cooldownMs must be >= 0.");

        var conditions = await AutomationValidationHelper.ValidateAndBuildConditionDefinitions(
            request.HomeId,
            request.Conditions,
            _context,
            _capabilityRegistry,
            cancellationToken);
        var timeWindow = AutomationValidationHelper.ValidateAndBuildTimeWindowDefinition(request.TimeWindow);

        var actionSet = await ActionSetValidationHelper.ValidateAndBuildDefinition(
            request.HomeId,
            request.ActionSet,
            _context,
            _capabilityRegistry,
            _capabilityStateValidator,
            _capabilityCommandValidator,
            cancellationToken);

        var rule = AutomationRule.Create(
            request.HomeId,
            request.Name,
            request.Description,
            request.IsEnabled,
            request.ConditionLogic,
            request.CooldownMs,
            conditions,
            timeWindow,
            actionSet);

        await _automationRuleRepository.Add(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtimePublisher.PublishToHome(
            rule.HomeId,
            AutomationRealtime.ForRule(rule, RealtimeChanges.Created),
            cancellationToken);
        return rule.Id;
    }
}
