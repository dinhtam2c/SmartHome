namespace Core.Domain.DeviceCategories;

public static class DeviceCategoryIds
{
    public const string Other = "other";

    public static string Normalize(string? category)
    {
        var normalized = category?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? Other : normalized;
    }
}

public sealed record DeviceCategoryDefinition(
    string Id,
    string DefaultName,
    string IconKey,
    string Color,
    int Order);
