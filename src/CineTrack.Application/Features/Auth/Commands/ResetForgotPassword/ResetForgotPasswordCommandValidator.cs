using FluentValidation;

namespace CineTrack.Application.Features.Auth.Commands.ResetForgotPassword;

public class ResetForgotPasswordCommandValidator : AbstractValidator<ResetForgotPasswordCommand>
{
    public ResetForgotPasswordCommandValidator()
    {
        RuleFor(x => x.TemporaryToken)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}
