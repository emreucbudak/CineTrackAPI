using FluentValidation;

namespace CineTrack.Application.Features.Movies.Queries.Discover;

public class DiscoverMoviesQueryValidator : AbstractValidator<DiscoverMoviesQuery>
{
    public DiscoverMoviesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(500)
            .WithMessage("Page must be between 1 and 500.");
    }
}
