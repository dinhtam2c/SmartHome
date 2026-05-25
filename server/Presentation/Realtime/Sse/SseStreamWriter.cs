using System.Threading.Channels;

namespace Presentation.Realtime.Sse;

internal static class SseStreamWriter
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(20);

    public static async Task Stream(
        HttpContext context,
        Channel<SseMessage> channel,
        CancellationToken applicationStopping,
        Action onDisconnect)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache, no-transform");
        context.Response.Headers.Append("Connection", "keep-alive");
        context.Response.Headers.Append("X-Accel-Buffering", "no");

        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            applicationStopping);
        var cancellationToken = cancellationSource.Token;

        try
        {
            await context.Response.StartAsync(cancellationToken);
            await WriteComment(context, "connected", cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var waitForMessage = channel.Reader.WaitToReadAsync(cancellationToken).AsTask();
                var heartbeat = Task.Delay(HeartbeatInterval, cancellationToken);
                var completed = await Task.WhenAny(waitForMessage, heartbeat);

                if (completed == heartbeat)
                {
                    await WriteComment(context, "heartbeat", cancellationToken);
                    continue;
                }

                if (!await waitForMessage)
                    break;

                while (channel.Reader.TryRead(out var message))
                {
                    await WriteMessage(context, message, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            onDisconnect();
        }
    }

    private static async Task WriteMessage(
        HttpContext context,
        SseMessage message,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(message.Id))
            await context.Response.WriteAsync($"id: {SanitizeLine(message.Id)}\n", cancellationToken);

        if (message.RetryMs.HasValue)
            await context.Response.WriteAsync($"retry: {message.RetryMs.Value}\n", cancellationToken);

        await context.Response.WriteAsync($"event: {SanitizeLine(message.Event)}\n", cancellationToken);

        foreach (var line in message.Data.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            await context.Response.WriteAsync($"data: {line}\n", cancellationToken);
        }

        await context.Response.WriteAsync("\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }

    private static async Task WriteComment(
        HttpContext context,
        string comment,
        CancellationToken cancellationToken)
    {
        await context.Response.WriteAsync($": {SanitizeLine(comment)}\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }

    private static string SanitizeLine(string value)
    {
        return value.Replace('\r', ' ').Replace('\n', ' ');
    }
}
