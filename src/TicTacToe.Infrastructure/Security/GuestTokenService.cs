using System.Security.Cryptography;
using System.Text;
using TicTacToe.Application.Abstractions.Security;

namespace TicTacToe.Infrastructure.Security;

public class GuestTokenService : IGuestTokenService
{
    public GuestTokenIssue Issue()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        // Base64Url encode without padding
        var rawToken = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var tokenHash = Hash(rawToken);

        return new GuestTokenIssue(rawToken, tokenHash);
    }

    public string Hash(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
