using FluentValidation;

namespace CineTrack.Application.Features.Movies.Queries.GetTrending;

public class GetTrendingMoviesQueryValidator : AbstractValidator<GetTrendingMoviesQuery>
{
    private static readonly string[] AllowedTimeWindows = ["day", "week"];

    public GetTrendingMoviesQueryValidator()
    {
        RuleFor(x => x.TimeWindow)
            .Must(tw => AllowedTimeWindows.Contains(tw))
            .WithMessage("TimeWindow must be 'day' or 'week'.");
    }
}
