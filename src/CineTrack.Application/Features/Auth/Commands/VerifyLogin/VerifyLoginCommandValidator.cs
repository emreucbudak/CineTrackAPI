using FluentValidation;

namespace CineTrack.Application.Features.Auth.Commands.VerifyLogin;

public class VerifyLoginCommandValidator : AbstractValidator<VerifyLoginCommand>
{
    public VerifyLoginCommandValidator()
    {
        RuleFor(x => x.TemporaryToken)
            .NotEmpty().WithMessage("Giriş doğrulama oturumu bulunamadı. Lütfen tekrar giriş yapın.");

        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Doğrulama kodunu girin.")
            .Length(6).WithMessage("Doğrulama kodu 6 haneli olmalıdır.")
            .Matches("^[0-9]{6}$").WithMessage("Doğrulama kodu yalnızca rakamlardan oluşmalıdır.");
    }
}
