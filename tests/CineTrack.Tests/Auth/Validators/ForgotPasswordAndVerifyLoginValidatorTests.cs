using CineTrack.Application.Features.Auth.Commands.ForgotPassword;
using CineTrack.Application.Features.Auth.Commands.VerifyLogin;
using FluentAssertions;

namespace CineTrack.Tests.Auth.Validators;

public class ForgotPasswordAndVerifyLoginValidatorTests
{
    [Fact]
    public void ForgotPasswordValidator_ShouldFail_WhenEmailExceedsMaxLength()
    {
        var validator = new ForgotPasswordCommandValidator();
        var email = $"{new string('a', 245)}@example.com";
        var command = new ForgotPasswordCommand(email);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "E-posta adresi en fazla 256 karakter olabilir.");
    }

    [Fact]
    public void VerifyLoginValidator_ShouldFail_WhenCodeContainsNonDigitCharacters()
    {
        var validator = new VerifyLoginCommandValidator();
        var command = new VerifyLoginCommand("temporary-token", "12A45B");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "Doğrulama kodu yalnızca rakamlardan oluşmalıdır.");
    }

    [Fact]
    public void VerifyLoginValidator_ShouldFail_WhenCodeLengthIsNotSix()
    {
        var validator = new VerifyLoginCommandValidator();
        var command = new VerifyLoginCommand("temporary-token", "12345");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "Doğrulama kodu 6 haneli olmalıdır.");
    }
}
