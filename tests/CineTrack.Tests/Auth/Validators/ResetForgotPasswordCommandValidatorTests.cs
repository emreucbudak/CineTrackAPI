using CineTrack.Application.Features.Auth.Commands.ResetForgotPassword;
using FluentAssertions;

namespace CineTrack.Tests.Auth.Validators;

public class ResetForgotPasswordCommandValidatorTests
{
    private readonly ResetForgotPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenPasswordContainsLettersAndDigitsOnly()
    {
        var command = new ResetForgotPasswordCommand("temporary-token", "yenisifre123");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTemporaryTokenIsMissing()
    {
        var command = new ResetForgotPasswordCommand(string.Empty, "yenisifre123");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "Şifre yenileme oturumu bulunamadı. Lütfen işlemi yeniden başlatın.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordContainsSpecialCharacter()
    {
        var command = new ResetForgotPasswordCommand("temporary-token", "yenisifre123!");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "Şifre yalnızca harf ve rakamlardan oluşmalıdır.");
    }
}
