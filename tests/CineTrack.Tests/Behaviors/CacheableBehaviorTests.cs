using CineTrack.Application.Abstractions;
using CineTrack.Application.Behaviors;
using CineTrack.Domain.Shared;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace CineTrack.Tests.Behaviors;

public class CacheableBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldReturnCachedResultValue_WhenGenericResultExistsInCache()
    {
        var cache = new FakeCacheService();
        cache.Store["movies:1"] = 42;
        var sut = CreateResultBehavior(cache);
        var nextWasCalled = false;

        var response = await sut.Handle(
            new CachedResultQuery("movies:1", TimeSpan.FromMinutes(10)),
            _ =>
            {
                nextWasCalled = true;
                return Task.FromResult(Result.Success(99));
            },
            CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be(42);
        nextWasCalled.Should().BeFalse();
        cache.SetInvocations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCacheInnerValue_WhenGenericResultCacheMissAndHandlerSucceeds()
    {
        var cache = new FakeCacheService();
        var sut = CreateResultBehavior(cache);

        var response = await sut.Handle(
            new CachedResultQuery("movies:2", TimeSpan.FromMinutes(5)),
            _ => Task.FromResult(Result.Success(88)),
            CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be(88);
        cache.SetInvocations.Should().ContainSingle();
        cache.SetInvocations.Single().Key.Should().Be("movies:2");
        cache.SetInvocations.Single().Value.Should().Be(88);
        cache.SetInvocations.Single().Expiration.Should().Be(TimeSpan.FromMinutes(5));
        cache.Store["movies:2"].Should().Be(88);
    }

    [Fact]
    public async Task Handle_ShouldSkipCaching_WhenGenericResultHandlerFails()
    {
        var cache = new FakeCacheService();
        var sut = CreateResultBehavior(cache);
        var failure = Result.Failure<int>(Error.Validation("Movies.Invalid", "Film bulunamadı."));

        var response = await sut.Handle(
            new CachedResultQuery("movies:3", TimeSpan.FromMinutes(5)),
            _ => Task.FromResult(failure),
            CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be(failure.Error);
        cache.SetInvocations.Should().BeEmpty();
        cache.Store.Should().NotContainKey("movies:3");
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedResponse_WhenNonResultResponseExistsInCache()
    {
        var cache = new FakeCacheService();
        cache.Store["movies:text:1"] = "cached-response";
        var sut = CreateStringBehavior(cache);
        var nextWasCalled = false;

        var response = await sut.Handle(
            new CachedStringQuery("movies:text:1", TimeSpan.FromMinutes(5)),
            _ =>
            {
                nextWasCalled = true;
                return Task.FromResult("fresh-response");
            },
            CancellationToken.None);

        response.Should().Be("cached-response");
        nextWasCalled.Should().BeFalse();
        cache.SetInvocations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCacheResponse_WhenNonResultResponseCacheMiss()
    {
        var cache = new FakeCacheService();
        var sut = CreateStringBehavior(cache);

        var response = await sut.Handle(
            new CachedStringQuery("movies:text:2", TimeSpan.FromMinutes(7)),
            _ => Task.FromResult("fresh-response"),
            CancellationToken.None);

        response.Should().Be("fresh-response");
        cache.SetInvocations.Should().ContainSingle();
        cache.SetInvocations.Single().Key.Should().Be("movies:text:2");
        cache.SetInvocations.Single().Value.Should().Be("fresh-response");
        cache.SetInvocations.Single().Expiration.Should().Be(TimeSpan.FromMinutes(7));
    }

    private static CacheableBehavior<CachedResultQuery, Result<int>> CreateResultBehavior(FakeCacheService cache) =>
        new(cache, NullLogger<CacheableBehavior<CachedResultQuery, Result<int>>>.Instance);

    private static CacheableBehavior<CachedStringQuery, string> CreateStringBehavior(FakeCacheService cache) =>
        new(cache, NullLogger<CacheableBehavior<CachedStringQuery, string>>.Instance);

    private sealed record CachedResultQuery(string CacheKey, TimeSpan? CacheDuration)
        : IRequest<Result<int>>, ICacheableQuery;

    private sealed record CachedStringQuery(string CacheKey, TimeSpan? CacheDuration)
        : IRequest<string>, ICacheableQuery;

    private sealed class FakeCacheService : ICacheService
    {
        public Dictionary<string, object?> Store { get; } = new();
        public List<SetInvocation> SetInvocations { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (!Store.TryGetValue(key, out var value))
            {
                return Task.FromResult(default(T));
            }

            return Task.FromResult((T?)value);
        }

        public Task<object?> GetAsync(string key, Type valueType, CancellationToken cancellationToken = default)
        {
            if (!Store.TryGetValue(key, out var value))
            {
                return Task.FromResult<object?>(null);
            }

            return Task.FromResult(valueType.IsInstanceOfType(value) ? value : null);
        }

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
        {
            Store[key] = value;
            SetInvocations.Add(new SetInvocation(key, value, expiration));
            return Task.CompletedTask;
        }

        public Task SetAsync(
            string key,
            object value,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
        {
            Store[key] = value;
            SetInvocations.Add(new SetInvocation(key, value, expiration));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Store.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed record SetInvocation(string Key, object? Value, TimeSpan? Expiration);
}
