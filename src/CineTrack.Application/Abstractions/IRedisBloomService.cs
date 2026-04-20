namespace CineTrack.Application.Abstractions;

public interface IRedisBloomService
{
    Task<bool> ExistsAsync(string key, string item, CancellationToken cancellationToken = default);
    Task<bool> AddAsync(string key, string item, CancellationToken cancellationToken = default);
}
