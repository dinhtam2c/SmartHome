namespace Presentation.Realtime.Sse;

internal sealed record SseMessage(
    string Event,
    string Data,
    string Id = "",
    int? RetryMs = null);
