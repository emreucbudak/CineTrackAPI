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
            var movieTask = _httpClient.GetAsync($"movie/{tmdbId}", cancellationToken);
            var creditsTask = _httpClient.GetAsync($"movie/{tmdbId}/credits", cancellationToken);

            await Task.WhenAll(movieTask, creditsTask);

            var movieResponse = movieTask.Result;
            var creditsResponse = creditsTask.Result;

            if (movieResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result.Failure<MovieDetailDto>(Error.NotFound("Movie", tmdbId));

            movieResponse.EnsureSuccessStatusCode();
            creditsResponse.EnsureSuccessStatusCode();

            var movie = await movieResponse.Content.ReadFromJsonAsync<TmdbMovieResponse>(cancellationToken);
            var credits = await creditsResponse.Content.ReadFromJsonAsync<TmdbCreditsResponse>(cancellationToken);

            var cast = credits!.Cast
                .OrderBy(c => c.Order)
                .Take(20)
                .Select(c => new CastMemberDto(c.Id, c.Name, c.Character, c.ProfilePath, c.Order))
                .ToList();

            return new MovieDetailDto(
                movie!.Id,
                movie.Title,
                movie.Overview,
                movie.PosterPath,
                movie.BackdropPath,
                movie.ReleaseDate,
                movie.VoteAverage,
                movie.VoteCount,
                movie.Genres.Select(g => new GenreDto(g.Id, g.Name)).ToList(),
                cast);
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
            var personTask = _httpClient.GetAsync($"person/{personId}", cancellationToken);
            var creditsTask = _httpClient.GetAsync($"person/{personId}/movie_credits", cancellationToken);

            await Task.WhenAll(personTask, creditsTask);

            var personResponse = personTask.Result;
            var creditsResponse = creditsTask.Result;

            if (personResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result.Failure<PersonDetailDto>(Error.NotFound("Person", personId));

            personResponse.EnsureSuccessStatusCode();
            creditsResponse.EnsureSuccessStatusCode();

            var person = await personResponse.Content.ReadFromJsonAsync<TmdbPersonResponse>(cancellationToken);
            var credits = await creditsResponse.Content.ReadFromJsonAsync<TmdbPersonCreditsResponse>(cancellationToken);

            var movieCredits = credits!.Cast
                .Where(c => !string.IsNullOrEmpty(c.Title))
                .OrderByDescending(c => c.ReleaseDate ?? "")
                .Take(50)
                .Select(c => new MovieCreditDto(c.Id, c.Title, c.Character, c.PosterPath, c.ReleaseDate, c.VoteAverage))
                .ToList();

            return new PersonDetailDto(
                person!.Id,
                person.Name,
                person.Biography,
                person.ProfilePath,
                person.Birthday,
                person.PlaceOfBirth,
                person.KnownForDepartment,
                movieCredits);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "TMDb API error fetching person {PersonId}", personId);
            return Result.Failure<PersonDetailDto>(Error.Failure("Tmdb.RequestFailed", "Failed to fetch person from TMDb."));
        }
    }

    public async Task<Result<List<DiscoverMovieDto>>> DiscoverMoviesAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"discover/movie?sort_by=popularity.desc&include_adult=false&vote_count.gte=100&page={page}",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var discover = await response.Content.ReadFromJsonAsync<TmdbDiscoverResponse>(cancellationToken);

            var movies = discover!.Results.Select(m => new DiscoverMovieDto(
                m.Id, m.Title, m.Overview, m.PosterPath, m.ReleaseDate, m.VoteAverage)).ToList();

            return movies;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "TMDb API error fetching discover movies");
            return Result.Failure<List<DiscoverMovieDto>>(Error.Failure("Tmdb.RequestFailed", "Failed to fetch discover movies from TMDb."));
        }
    }
}
