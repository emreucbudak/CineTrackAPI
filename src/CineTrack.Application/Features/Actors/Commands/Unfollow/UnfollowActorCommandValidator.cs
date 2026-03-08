using FluentValidation;

namespace CineTrack.Application.Features.Actors.Commands.Unfollow;

public class UnfollowActorCommandValidator : AbstractValidator<UnfollowActorCommand>
{
    public UnfollowActorCommandValidator()
    {
        RuleFor(x => x.TmdbId)
            .GreaterThan(0);

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
