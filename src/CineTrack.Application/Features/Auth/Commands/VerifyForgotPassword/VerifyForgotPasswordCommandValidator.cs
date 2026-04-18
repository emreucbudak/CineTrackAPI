using FluentValidation;

namespace CineTrack.Application.Features.Auth.Commands.VerifyForgotPassword;

public class VerifyForgotPasswordCommandValidator : AbstractValidator<VerifyForgotPasswordCommand>
{
    public VerifyForgotPasswordCommandValidator()
    {
        RuleFor(x => x.TemporaryToken)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$");
    }
}
