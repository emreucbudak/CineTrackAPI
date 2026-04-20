using FluentValidation;

namespace CineTrack.Application.Features.Auth.Commands.ResetForgotPassword;

public class ResetForgotPasswordCommandValidator : AbstractValidator<ResetForgotPasswordCommand>
{
    public ResetForgotPasswordCommandValidator()
    {
        RuleFor(x => x.TemporaryToken)
            .NotEmpty().WithMessage("Şifre yenileme oturumu bulunamadı. Lütfen işlemi yeniden başlatın.");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Yeni şifrenizi girin.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter olabilir.")
            .Matches(@"^[\p{L}\p{Nd}]+$").WithMessage("Şifre yalnızca harf ve rakamlardan oluşmalıdır.")
            .Matches(@"\p{L}").WithMessage("Şifre en az bir harf içermelidir.")
            .Matches(@"\p{Nd}").WithMessage("Şifre en az bir rakam içermelidir.");
    }
}
