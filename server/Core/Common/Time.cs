namespace Core.Common;

public static class Time
{
    public static long UnixNow()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
