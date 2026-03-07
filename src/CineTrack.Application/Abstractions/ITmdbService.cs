using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;

namespace CineTrack.Application.Abstractions;

public interface ITmdbService
{
    Task<Result<MovieDetailDto>> GetMovieDetailAsync(int tmdbId, CancellationToken cancellationToken = default);
    Task<Result<List<TrendingMovieDto>>> GetTrendingMoviesAsync(string timeWindow = "day", CancellationToken cancellationToken = default);
    Task<Result<PersonDetailDto>> GetPersonDetailAsync(int personId, CancellationToken cancellationToken = default);
}
