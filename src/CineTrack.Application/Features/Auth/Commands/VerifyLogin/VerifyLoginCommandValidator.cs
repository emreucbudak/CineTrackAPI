using FluentValidation;

namespace CineTrack.Application.Features.Auth.Commands.VerifyLogin;

public class VerifyLoginCommandValidator : AbstractValidator<VerifyLoginCommand>
{
    public VerifyLoginCommandValidator()
    {
        RuleFor(x => x.TemporaryToken)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$");
    }
}
