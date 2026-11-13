using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Tactics.Api.Games;

/// <summary>
/// The secrets that replace accounts: one token per seat, one invite code per game. Whoever holds a token plays
/// that seat (a capability), so tokens are unguessable and the server keeps only their hash.
/// </summary>
public static class PlayerSecrets
{
    /// <summary>The characters of an invite code: capitals and digits without look-alikes (0/O, 1/I/L).</summary>
    public const string InviteCodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <summary>How many characters an invite code has: 31^8, about 40 bits, guessed only together with the game id.</summary>
    public const int InviteCodeLength = 8;

    // 256 bits from the operating system's secure random generator: nobody can guess or brute-force it.
    private const int TokenBytes = 32;

    /// <summary>A new player token: 32 random bytes as base64url (43 characters, safe in headers and URLs).</summary>
    public static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));

    /// <summary>
    /// The SHA-256 of a token, as lowercase hex. A fast hash is enough: unlike a password, a random 256-bit
    /// token can't be guessed. If the store ever leaks, the hashes don't let anyone play.
    /// </summary>
    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>
    /// True if the token hashes to the stored hash. The comparison takes the same time wherever the bytes differ,
    /// so response times leak nothing about the stored hash.
    /// </summary>
    public static bool Matches(string token, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(token);
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(storedHash));
    }

    /// <summary>A new invite code, such as "K7QF3MZP".</summary>
    public static string NewInviteCode() => RandomNumberGenerator.GetString(InviteCodeAlphabet, InviteCodeLength);

    /// <summary>
    /// True if what someone typed is the game's invite code, ignoring case and surrounding spaces, because people
    /// type it. Constant time too.
    /// </summary>
    public static bool InviteCodeMatches(string attempt, string inviteCode)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(inviteCode);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(attempt.Trim().ToUpperInvariant()),
            Encoding.UTF8.GetBytes(inviteCode));
    }
}
