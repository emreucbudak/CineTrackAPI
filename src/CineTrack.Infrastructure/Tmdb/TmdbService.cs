using System.Net;
using System.Net.Http.Json;
using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace CineTrack.Infrastructure.Tmdb;

public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbService> _logger;

    public TmdbService(HttpClient httpClient, ILogger<TmdbService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<MovieDetailDto>> GetMovieDetailAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"movie/{tmdbId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result.Failure<MovieDetailDto>(Error.NotFound("Movie", tmdbId));

            response.EnsureSuccessStatusCode();

            var movie = await response.Content.ReadFromJsonAsync<TmdbMovieResponse>(cancellationToken);

            return new MovieDetailDto(
                movie!.Id,
                movie.Title,
                movie.Overview,
                movie.PosterPath,
                movie.BackdropPath,
                movie.ReleaseDate,
                movie.VoteAverage,
                movie.VoteCount,
                movie.Genres.Select(g => new GenreDto(g.Id, g.Name)).ToList());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "TMDb API error fetching movie {TmdbId}", tmdbId);
            return Result.Failure<MovieDetailDto>(Error.Failure("Tmdb.RequestFailed", "Failed to fetch movie from TMDb."));
        }
    }

    public async Task<Result<List<TrendingMovieDto>>> GetTrendingMoviesAsync(string timeWindow = "day", CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"trending/movie/{timeWindow}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var trending = await response.Content.ReadFromJsonAsync<TmdbTrendingResponse>(cancellationToken);

            var movies = trending!.Results.Select(m => new TrendingMovieDto(
                m.Id, m.Title, m.Overview, m.PosterPath, m.ReleaseDate, m.VoteAverage)).ToList();

            return movies;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "TMDb API error fetching trending movies");
            return Result.Failure<List<TrendingMovieDto>>(Error.Failure("Tmdb.RequestFailed", "Failed to fetch trending movies from TMDb."));
        }
    }

    public async Task<Result<PersonDetailDto>> GetPersonDetailAsync(int personId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"person/{personId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result.Failure<PersonDetailDto>(Error.NotFound("Person", personId));

            response.EnsureSuccessStatusCode();

            var person = await response.Content.ReadFromJsonAsync<TmdbPersonResponse>(cancellationToken);

            return new PersonDetailDto(
                person!.Id,
                person.Name,
                person.Biography,
                person.ProfilePath,
                person.Birthday,
                person.PlaceOfBirth,
                person.KnownForDepartment);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "TMDb API error fetching person {PersonId}", personId);
            return Result.Failure<PersonDetailDto>(Error.Failure("Tmdb.RequestFailed", "Failed to fetch person from TMDb."));
        }
    }
}
