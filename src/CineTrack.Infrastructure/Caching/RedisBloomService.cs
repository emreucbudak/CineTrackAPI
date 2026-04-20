using CineTrack.Application.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CineTrack.Infrastructure.Caching;

public class RedisBloomService : IRedisBloomService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisBloomService> _logger;

    public RedisBloomService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisBloomService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(string key, string item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var result = await database.ExecuteAsync("BF.EXISTS", key, item);
            return (int)result == 1;
        }
        catch (RedisServerException ex)
        {
            _logger.LogError(ex, "RedisBloom BF.EXISTS failed for key {Key}.", key);
            throw new InvalidOperationException(
                "RedisBloom BF.EXISTS failed. Ensure the RedisBloom module is available in Redis.",
                ex);
        }
    }

    public async Task<bool> AddAsync(string key, string item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var result = await database.ExecuteAsync("BF.ADD", key, item);
            return (int)result == 1;
        }
        catch (RedisServerException ex)
        {
            _logger.LogError(ex, "RedisBloom BF.ADD failed for key {Key}.", key);
            throw new InvalidOperationException(
                "RedisBloom BF.ADD failed. Ensure the RedisBloom module is available in Redis.",
                ex);
        }
    }
}
