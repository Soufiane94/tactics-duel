using Tactics.Api.Games;

namespace Tactics.Api.Tests.Games;

public sealed class PlayerSecretsTests
{
    [Fact]
    public void A_token_is_43_base64url_characters()
    {
        // 32 bytes = 256 bits. Base64url uses letters, digits, '-' and '_', so it fits in a header or a URL as is.
        Assert.Matches("^[A-Za-z0-9_-]{43}$", PlayerSecrets.NewToken());
    }

    [Fact]
    public void Tokens_never_repeat()
    {
        var tokens = Enumerable.Range(0, 1000).Select(_ => PlayerSecrets.NewToken()).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(1000, tokens.Count);
    }

    [Fact]
    public void A_token_matches_its_own_hash_and_nothing_else()
    {
        var hash = PlayerSecrets.Hash("token-one");

        Assert.True(PlayerSecrets.Matches("token-one", hash));
        Assert.False(PlayerSecrets.Matches("token-two", hash));
        Assert.False(PlayerSecrets.Matches("TOKEN-ONE", hash)); // tokens are case-sensitive
        Assert.False(PlayerSecrets.Matches("", hash));
    }

    [Fact]
    public void The_stored_hash_is_64_hex_characters_and_does_not_contain_the_token()
    {
        var token = PlayerSecrets.NewToken();

        var hash = PlayerSecrets.Hash(token);

        Assert.Matches("^[0-9a-f]{64}$", hash);
        Assert.DoesNotContain(token, hash, StringComparison.Ordinal);
    }

    [Fact]
    public void An_invite_code_has_8_characters_without_look_alikes()
    {
        foreach (var lookAlike in "0O1IL")
        {
            Assert.DoesNotContain(lookAlike, PlayerSecrets.InviteCodeAlphabet);
        }

        for (var i = 0; i < 200; i++)
        {
            var code = PlayerSecrets.NewInviteCode();
            Assert.Equal(PlayerSecrets.InviteCodeLength, code.Length);
            Assert.All(code, c => Assert.Contains(c, PlayerSecrets.InviteCodeAlphabet));
        }
    }

    [Theory]
    [InlineData("K7QF3MZP", true)]
    [InlineData("k7qf3mzp", true)]
    [InlineData(" K7QF3MZP ", true)]
    [InlineData("K7QF3MZQ", false)]
    [InlineData("K7QF3MZ", false)]
    [InlineData("", false)]
    public void An_invite_code_matches_ignoring_case_and_surrounding_spaces(string attempt, bool expected)
    {
        Assert.Equal(expected, PlayerSecrets.InviteCodeMatches(attempt, "K7QF3MZP"));
    }
}
