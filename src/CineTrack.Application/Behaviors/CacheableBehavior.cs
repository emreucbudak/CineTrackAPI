using CineTrack.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CineTrack.Application.Behaviors;

public class CacheableBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableQuery
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheableBehavior<TRequest, TResponse>> _logger;

    public CacheableBehavior(ICacheService cache, ILogger<CacheableBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;

        var cached = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogInformation("Cache miss for {CacheKey}", cacheKey);

        var response = await next(cancellationToken);

        await _cache.SetAsync(cacheKey, response, request.CacheDuration, cancellationToken);

        return response;
    }
}
