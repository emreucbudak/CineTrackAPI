using FluentValidation;

namespace CineTrack.Application.Features.Actors.Queries.Search;

public class SearchActorsQueryValidator : AbstractValidator<SearchActorsQuery>
{
    public SearchActorsQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Page)
            .GreaterThan(0);
    }
}
