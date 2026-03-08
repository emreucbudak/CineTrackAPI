using FluentValidation;

namespace CineTrack.Application.Features.Movies.Commands.AddFavorite;

public class AddFavoriteMovieCommandValidator : AbstractValidator<AddFavoriteMovieCommand>
{
    public AddFavoriteMovieCommandValidator()
    {
        RuleFor(x => x.TmdbId)
            .GreaterThan(0);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
