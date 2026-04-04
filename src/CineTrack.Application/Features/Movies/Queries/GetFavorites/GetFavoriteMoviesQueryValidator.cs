using FluentValidation;

namespace CineTrack.Application.Features.Movies.Queries.GetFavorites;

public class GetFavoriteMoviesQueryValidator : AbstractValidator<GetFavoriteMoviesQuery>
{
    public GetFavoriteMoviesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
