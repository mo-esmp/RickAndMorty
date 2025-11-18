using Application.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Cache;

public sealed class DistributedAppCache(
    IDistributedCache inner,
    ILogger<DistributedAppCache> logger,
    IOptions<AppCacheOptions> options)
    : IAppCache
{
    private const byte FlagJson = 0x01;
    private const byte FlagJsonGzip = 0x02;
    private const byte FlagUtf8 = 0x03;       // raw UTF-8 string (uncompressed)
    private const byte FlagUtf8Gzip = 0x04;

    private readonly AppCacheOptions _appCacheDefaults = options.Value;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public byte[]? Get(string key) => inner.Get(key);

    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        //var data = await inner.GetAsync(key, token);
        //return data == null ? default : JsonSerializer.Deserialize<T>(data);

        byte[]? bytes = await inner.GetAsync(key, token);
        if (bytes is null || bytes.Length == 0)
        {
            return default;
        }

        return Deserialize<T>(bytes);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        => inner.GetAsync(key, token);

    public void Refresh(string key) => inner.Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default)
        => inner.RefreshAsync(key, token);

    public void Remove(string key) => inner.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default)
        => inner.RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => inner.Set(key, value, ApplyDefaults(options));

    public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        byte[] payload = Serialize(value);
        await inner.SetAsync(key, payload, ApplyDefaults(options), token);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        => inner.SetAsync(key, value, ApplyDefaults(options), token);

    private byte[] Serialize<T>(T value)
    {
        // Optimize strings a bit (avoid JSON quoting when not needed)
        if (value is string s)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(s);
            return utf8.Length >= _appCacheDefaults.CompressionThresholdBytes
                ? WithFlag(FlagUtf8Gzip, Gzip(utf8))
                : WithFlag(FlagUtf8, utf8);
        }

        byte[] json = JsonSerializer.SerializeToUtf8Bytes(value, _serializerOptions);
        return json.Length >= _appCacheDefaults.CompressionThresholdBytes
            ? WithFlag(FlagJsonGzip, Gzip(json))
            : WithFlag(FlagJson, json);
    }

    private T? Deserialize<T>(byte[] payload)
    {
        if (payload.Length == 0)
        {
            return default;
        }

        byte flag = payload[0];
        ReadOnlySpan<byte> span = new(payload, 1, payload.Length - 1);

        switch (flag)
        {
            case FlagUtf8:
                if (typeof(T) == typeof(string))
                    return (T)(object)Encoding.UTF8.GetString(span);
                // fall back: interpret utf8 as JSON
                return JsonSerializer.Deserialize<T>(span, _serializerOptions);

            case FlagUtf8Gzip:
                {
                    byte[] decompressed = Gunzip(span);
                    if (typeof(T) == typeof(string))
                        return (T)(object)Encoding.UTF8.GetString(decompressed);
                    return JsonSerializer.Deserialize<T>(decompressed, _serializerOptions);
                }

            case FlagJson:
                return JsonSerializer.Deserialize<T>(span, _serializerOptions);

            case FlagJsonGzip:
                {
                    byte[] decompressed = Gunzip(span);
                    return JsonSerializer.Deserialize<T>(decompressed, _serializerOptions);
                }

            default:
                // Unknown payload
                logger.LogWarning("Unknown cache payload flag {Flag}. Attempting JSON deserialization.", flag);
                return JsonSerializer.Deserialize<T>(span, _serializerOptions);
        }
    }

    private static byte[] WithFlag(byte flag, byte[] bytes)
    {
        byte[] result = new byte[1 + bytes.Length];
        result[0] = flag;
        Buffer.BlockCopy(bytes, 0, result, 1, bytes.Length);
        return result;
    }

    private static byte[] Gzip(ReadOnlySpan<byte> input)
    {
        using MemoryStream ms = new();
        using (GZipStream gz = new(ms, CompressionLevel.Fastest, leaveOpen: true))
            gz.Write(input);

        return ms.ToArray();
    }

    private static byte[] Gunzip(ReadOnlySpan<byte> input)
    {
        using MemoryStream src = new(input.ToArray());
        using GZipStream gz = new(src, CompressionMode.Decompress);
        using MemoryStream dst = new();
        gz.CopyTo(dst);

        return dst.ToArray();
    }

    private DistributedCacheEntryOptions ApplyDefaults(DistributedCacheEntryOptions? options)
    {
        DistributedCacheEntryOptions effective = options ?? new();

        // Only apply defaults if caller didn't specify anything
        if (effective.AbsoluteExpiration.HasValue || effective.SlidingExpiration.HasValue)
        {
            return effective;
        }

        if (_appCacheDefaults.AbsoluteExpiration.HasValue)
        {
            effective.SetAbsoluteExpiration(_appCacheDefaults.AbsoluteExpiration.Value);
        }
        else if (_appCacheDefaults.AbsoluteExpirationRelativeToNow.HasValue)
        {
            effective.SetAbsoluteExpiration(_appCacheDefaults.AbsoluteExpirationRelativeToNow.Value);
        }

        return effective;
    }
}