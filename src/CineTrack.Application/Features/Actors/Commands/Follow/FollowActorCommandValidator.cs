using FluentValidation;

namespace CineTrack.Application.Features.Actors.Commands.Follow;

public class FollowActorCommandValidator : AbstractValidator<FollowActorCommand>
{
    public FollowActorCommandValidator()
    {
        RuleFor(x => x.TmdbId)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
