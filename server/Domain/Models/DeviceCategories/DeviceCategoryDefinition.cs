namespace Domain.Models.DeviceCategories;

public sealed record DeviceCategoryDefinition(
    string Id,
    string DefaultName,
    string IconKey,
    string Color,
    int Order);
