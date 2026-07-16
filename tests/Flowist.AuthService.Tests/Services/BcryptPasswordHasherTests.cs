using Flowist.AuthService.Services;

using FluentAssertions;

namespace Flowist.AuthService.Tests.Services;

public sealed class BcryptPasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHash()
    {
        BcryptPasswordHasher hasher = new();

        string hash = hasher.HashPassword("Test123!");

        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HashPassword_ShouldNotReturnPlainPassword()
    {
        BcryptPasswordHasher hasher = new();

        string password = "Test123!";

        string hash = hasher.HashPassword(password);

        hash.Should().NotBe(password);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        BcryptPasswordHasher hasher = new();

        string password = "Test123!";
        string hash = hasher.HashPassword(password);

        bool result = hasher.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        BcryptPasswordHasher hasher = new();

        string hash = hasher.HashPassword("Test123!");

        bool result = hasher.VerifyPassword("WrongPassword123!", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashes_ForSamePassword()
    {
        BcryptPasswordHasher hasher = new();

        string password = "Test123!";

        string firstHash = hasher.HashPassword(password);
        string secondHash = hasher.HashPassword(password);

        firstHash.Should().NotBe(secondHash);

        hasher.VerifyPassword(password, firstHash).Should().BeTrue();
        hasher.VerifyPassword(password, secondHash).Should().BeTrue();
    }
}