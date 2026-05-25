namespace Domain.Models.Automations;

public sealed record AutomationTimeWindowDefinition(
    int StartMinute,
    int EndMinute,
    int DaysOfWeekMask);
