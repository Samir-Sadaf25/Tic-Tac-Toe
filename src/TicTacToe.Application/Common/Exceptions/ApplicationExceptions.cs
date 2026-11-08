namespace TicTacToe.Application.Common.Exceptions;

public class MatchNotFoundException : Exception
{
    public Guid MatchId { get; }

    public MatchNotFoundException(Guid matchId)
        : base($"Match with ID '{matchId}' was not found.")
    {
        MatchId = matchId;
    }
}

public class MatchAccessDeniedException : Exception
{
    public MatchAccessDeniedException(string message = "You do not have permission to access or modify this match.")
        : base(message)
    {
    }
}

public class MatchConflictException : Exception
{
    public MatchConflictException(string message = "The match state has changed. Please refresh and try again.")
        : base(message)
    {
    }
}
