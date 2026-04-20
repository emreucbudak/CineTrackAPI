using CineTrack.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CineTrack.Tests.Infrastructure.Auth;

public class PasswordFingerprintServiceTests
{
    [Fact]
    public void CreateFingerprint_ShouldBeDeterministic_ForSameSecretAndPassword()
    {
        var sut = CreateService("super-secret-key");

        var first = sut.CreateFingerprint("sifre123");
        var second = sut.CreateFingerprint("sifre123");

        first.Should().Be(second);
        first.Should().HaveLength(64);
    }

    [Fact]
    public void CreateFingerprint_ShouldChange_WhenSecretChanges()
    {
        var firstService = CreateService("secret-one");
        var secondService = CreateService("secret-two");

        var first = firstService.CreateFingerprint("sifre123");
        var second = secondService.CreateFingerprint("sifre123");

        first.Should().NotBe(second);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenJwtSecretIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var action = () => new PasswordFingerprintService(configuration);

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Jwt:Secret must be configured to create password bloom fingerprints.");
    }

    private static PasswordFingerprintService CreateService(string secret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret,
            })
            .Build();

        return new PasswordFingerprintService(configuration);
    }
}
