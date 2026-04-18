namespace WebAPI.Scenes;

public sealed record ExecuteSceneRequest(
    string? TriggerSource,
    IReadOnlyCollection<string>? OnlyEndpoints,
    IReadOnlyCollection<string>? ExcludeCapabilities
);
