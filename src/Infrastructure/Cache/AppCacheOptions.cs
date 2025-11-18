namespace Infrastructure.Cache;

public sealed class AppCacheOptions
{
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; } = TimeSpan.FromMinutes(5);

    public DateTimeOffset? AbsoluteExpiration { get; set; }

    public int CompressionThresholdBytes { get; set; } = 2 * 1024; // 2KB
}