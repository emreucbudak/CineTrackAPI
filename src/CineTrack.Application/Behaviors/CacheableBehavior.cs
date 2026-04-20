using CineTrack.Application.Abstractions;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CineTrack.Application.Behaviors;

public class CacheableBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableQuery
{
    private static readonly Type? ResultValueType = GetResultValueType();

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

        if (ResultValueType is not null)
        {
            var cachedValue = await _cache.GetAsync(cacheKey, ResultValueType, cancellationToken);
            if (cachedValue is not null)
            {
                _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
                return CreateSuccessfulResultResponse(cachedValue);
            }

            _logger.LogInformation("Cache miss for {CacheKey}", cacheKey);

            var resultResponse = await next(cancellationToken);

            if (resultResponse is Result failedResult && failedResult.IsFailure)
            {
                _logger.LogInformation("Skipping cache for failed response on {CacheKey}", cacheKey);
                return resultResponse;
            }

            var value = GetResultValue(resultResponse);
            if (value is not null)
            {
                await _cache.SetAsync(cacheKey, value, request.CacheDuration, cancellationToken);
            }

            return resultResponse;
        }

        var cached = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogInformation("Cache miss for {CacheKey}", cacheKey);

        var response = await next(cancellationToken);

        if (response is Result result && result.IsFailure)
        {
            _logger.LogInformation("Skipping cache for failed response on {CacheKey}", cacheKey);
            return response;
        }

        await _cache.SetAsync(cacheKey, response, request.CacheDuration, cancellationToken);

        return response;
    }

    private static Type? GetResultValueType()
    {
        var responseType = typeof(TResponse);

        if (!responseType.IsGenericType ||
            responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            return null;
        }

        return responseType.GetGenericArguments()[0];
    }

    private static object? GetResultValue(TResponse response) =>
        typeof(TResponse).GetProperty(nameof(Result<object>.Value))?.GetValue(response);

    private static TResponse CreateSuccessfulResultResponse(object value)
    {
        var successMethod = typeof(Result)
            .GetMethods()
            .Single(method =>
                method.Name == nameof(Result.Success) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 1);

        return (TResponse)successMethod
            .MakeGenericMethod(ResultValueType!)
            .Invoke(null, new[] { value })!;
    }
}
