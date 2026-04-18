using FluentValidation;

namespace CineTrack.Application.Features.Movies.Queries.Search;

public class SearchMoviesQueryValidator : AbstractValidator<SearchMoviesQuery>
{
    public SearchMoviesQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Page)
            .GreaterThan(0);
    }
}
