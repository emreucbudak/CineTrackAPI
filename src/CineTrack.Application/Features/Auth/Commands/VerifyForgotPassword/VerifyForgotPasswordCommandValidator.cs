using FluentValidation;

namespace CineTrack.Application.Features.Auth.Commands.VerifyForgotPassword;

public class VerifyForgotPasswordCommandValidator : AbstractValidator<VerifyForgotPasswordCommand>
{
    public VerifyForgotPasswordCommandValidator()
    {
        RuleFor(x => x.TemporaryToken)
            .NotEmpty().WithMessage("Şifre yenileme oturumu bulunamadı. Lütfen işlemi yeniden başlatın.");

        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Doğrulama kodunu girin.")
            .Length(6).WithMessage("Doğrulama kodu 6 haneli olmalıdır.")
            .Matches("^[0-9]{6}$").WithMessage("Doğrulama kodu yalnızca rakamlardan oluşmalıdır.");
    }
}
