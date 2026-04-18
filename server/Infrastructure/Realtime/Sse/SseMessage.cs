namespace Infrastructure.Realtime.Sse;

public sealed record SseMessage(string Event, string Data);
