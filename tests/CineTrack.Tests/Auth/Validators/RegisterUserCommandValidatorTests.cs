using CineTrack.Application.Features.Auth.Commands.Register;
using FluentAssertions;

namespace CineTrack.Tests.Auth.Validators;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _sut = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenPasswordContainsOnlyLettersAndDigits()
    {
        var command = new RegisterUserCommand(
            "emre@example.com",
            "emre",
            "sifre123");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordContainsSpecialCharacter()
    {
        var command = new RegisterUserCommand(
            "emre@example.com",
            "emre",
            "sifre123!");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "Şifre yalnızca harf ve rakamlardan oluşmalıdır.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordDoesNotContainDigit()
    {
        var command = new RegisterUserCommand(
            "emre@example.com",
            "emre",
            "sifresadece");

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "Şifre en az bir rakam içermelidir.");
    }
}
