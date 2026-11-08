namespace TicTacToe.Domain.Rules;

public sealed record WinningLine(int A, int B, int C);

public static class WinningLines
{
    public static readonly IReadOnlyList<WinningLine> Lines = new WinningLine[]
    {
        // Rows
        new(0, 1, 2),
        new(3, 4, 5),
        new(6, 7, 8),
        // Columns
        new(0, 3, 6),
        new(1, 4, 7),
        new(2, 5, 8),
        // Diagonals
        new(0, 4, 8),
        new(2, 4, 6)
    };

    public static bool IsWinningBoard(string board, char symbol, out WinningLine? winningLine)
    {
        foreach (var line in Lines)
        {
            if (board[line.A] == symbol && board[line.B] == symbol && board[line.C] == symbol)
            {
                winningLine = line;
                return true;
            }
        }

        winningLine = null;
        return false;
    }
}
