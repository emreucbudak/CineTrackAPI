using CineTrack.Infrastructure.Auth;
using FluentAssertions;

namespace CineTrack.Tests.Infrastructure.Auth;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldCreateVerifiableHash()
    {
        const string password = "sifre123";

        var hash = _sut.Hash(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
        _sut.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForDifferentPassword()
    {
        var hash = _sut.Hash("sifre123");

        var isValid = _sut.Verify("baskasifre456", hash);

        isValid.Should().BeFalse();
    }
}
