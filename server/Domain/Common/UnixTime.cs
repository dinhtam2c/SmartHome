namespace Domain.Common;

public static class UnixTime
{
    public static long Now()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
