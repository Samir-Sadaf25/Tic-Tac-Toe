using NUnit.Framework;
using Shouldly;
using TicTacToe.Infrastructure.Security;

namespace TicTacToe.IntegrationTests.Security;

[TestFixture]
public class GuestTokenServiceTests
{
    private readonly GuestTokenService _tokenService = new();

    [Test]
    public void Issue_GeneratesUniqueTokensWithMatchingHash()
    {
        var issue1 = _tokenService.Issue();
        var issue2 = _tokenService.Issue();

        issue1.RawToken.ShouldNotBeNullOrWhiteSpace();
        issue1.TokenHash.ShouldNotBeNullOrWhiteSpace();
        issue1.RawToken.ShouldNotBe(issue1.TokenHash);

        issue2.RawToken.ShouldNotBe(issue1.RawToken);
        issue2.TokenHash.ShouldNotBe(issue1.TokenHash);

        // Hashing the raw token must reproduce the exact token hash
        var computedHash = _tokenService.Hash(issue1.RawToken);
        computedHash.ShouldBe(issue1.TokenHash);
    }

    [Test]
    public void Hash_IsDeterministic()
    {
        const string rawToken = "sample-guest-token-12345";
        var hash1 = _tokenService.Hash(rawToken);
        var hash2 = _tokenService.Hash(rawToken);

        hash1.ShouldBe(hash2);
        hash1.Length.ShouldBe(64); // SHA-256 hex string length
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Hash_NullOrEmptyToken_ReturnsEmptyString(string? rawToken)
    {
        var hash = _tokenService.Hash(rawToken!);
        hash.ShouldBe(string.Empty);
    }

    [Test]
    public void Issue_ProducesUrlSafeFortyThreeCharacterTokenAndLowercaseHash()
    {
        var issue = _tokenService.Issue();

        // 32 random bytes -> 256 bits -> 43 unpadded base64url characters.
        issue.RawToken.Length.ShouldBe(43);
        issue.RawToken.ShouldMatch("^[A-Za-z0-9_-]{43}$");
        issue.RawToken.ShouldNotContain("+");
        issue.RawToken.ShouldNotContain("/");
        issue.RawToken.ShouldNotContain("=");

        issue.TokenHash.Length.ShouldBe(64);
        issue.TokenHash.ShouldMatch("^[0-9a-f]{64}$");
    }

    [Test]
    public void Issue_AcrossManyCalls_ProducesUniqueRawTokensAndHashes()
    {
        const int count = 1000;

        var issues = Enumerable.Range(0, count)
            .Select(_ => _tokenService.Issue())
            .ToList();

        issues.Select(i => i.RawToken).Distinct().Count().ShouldBe(count);
        issues.Select(i => i.TokenHash).Distinct().Count().ShouldBe(count);
        issues.ShouldAllBe(i => i.TokenHash == _tokenService.Hash(i.RawToken));
    }

    [Test]
    public void Hash_MatchesKnownSha256Vector()
    {
        // SHA-256("abc") = ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad
        _tokenService.Hash("abc").ShouldBe(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }
}
