namespace TicTacToe.Application.Abstractions.Security;

public sealed record GuestTokenIssue(string RawToken, string TokenHash);

public interface IGuestTokenService
{
    GuestTokenIssue Issue();
    string Hash(string rawToken);
}
