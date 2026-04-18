using FluentValidation;

namespace CineTrack.Application.Features.Auth.Commands.VerifyRegister;

public class VerifyRegisterCommandValidator : AbstractValidator<VerifyRegisterCommand>
{
    public VerifyRegisterCommandValidator()
    {
        RuleFor(x => x.TemporaryToken)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$");
    }
}
