using FluentValidation;

namespace CineTrack.Application.Features.Movies.Commands.RemoveFavorite;

public class RemoveFavoriteMovieCommandValidator : AbstractValidator<RemoveFavoriteMovieCommand>
{
    public RemoveFavoriteMovieCommandValidator()
    {
        RuleFor(x => x.TmdbId)
            .GreaterThan(0);

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
