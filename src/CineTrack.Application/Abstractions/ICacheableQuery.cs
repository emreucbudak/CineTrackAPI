namespace CineTrack.Application.Abstractions;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? CacheDuration { get; }
}
