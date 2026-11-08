namespace TicTacToe.Domain.Enums;

public enum GameErrorCode
{
    InvalidDisplayName = 1,
    InvalidSymbol = 2,
    MatchNotWaiting = 3,
    MatchAlreadyFull = 4,
    MatchNotInProgress = 5,
    MatchCompleted = 6,
    InvalidCellIndex = 7,
    CellAlreadyOccupied = 8,
    NotPlayerTurn = 9,
    PlayerNotFound = 10,
    CannotJoinOwnMatch = 11,
    InvalidMatchId = 12,
    InvalidPlayerId = 13,
    InvalidTokenHash = 14
}
