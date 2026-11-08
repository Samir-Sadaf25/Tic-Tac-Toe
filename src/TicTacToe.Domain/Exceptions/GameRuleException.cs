using TicTacToe.Domain.Enums;

namespace TicTacToe.Domain.Exceptions;

public class GameRuleException : Exception
{
    public GameErrorCode ErrorCode { get; }

    public GameRuleException(GameErrorCode errorCode, string? message = null)
        : base(message ?? $"Game rule violation: {errorCode}")
    {
        ErrorCode = errorCode;
    }
}
