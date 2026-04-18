using System.Threading.Channels;
using Infrastructure.Realtime.Sse;

namespace WebAPI.Realtime;

public static class SseStreamWriter
{
    public static async Task Stream(
        HttpContext context,
        Channel<SseMessage> channel,
        Action onDisconnect)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        try
        {
            await foreach (var message in channel.Reader.ReadAllAsync(context.RequestAborted))
            {
                await context.Response.WriteAsync($"event: {message.Event}\n");
                await context.Response.WriteAsync($"data: {message.Data}\n\n");
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        }
        finally
        {
            onDisconnect();
        }
    }
}