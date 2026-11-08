using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Rules;

namespace TicTacToe.Domain.Tests.Rules;

[TestFixture]
public class WinningLinesTests
{
    [Test]
    public void Lines_ShouldContainExactlyEightUniqueLines()
    {
        WinningLines.Lines.Count.ShouldBe(8);
        WinningLines.Lines.Distinct().Count().ShouldBe(8);
    }

    [TestCase("XXX......", 'X', true)]
    [TestCase("...XXX...", 'X', true)]
    [TestCase("......XXX", 'X', true)]
    [TestCase("X..X..X..", 'X', true)]
    [TestCase(".X..X..X.", 'X', true)]
    [TestCase("..X..X..X", 'X', true)]
    [TestCase("X...X...X", 'X', true)]
    [TestCase("..X.X.X..", 'X', true)]
    [TestCase("OOO......", 'O', true)]
    [TestCase("XXO......", 'X', false)]
    [TestCase(".........", 'X', false)]
    [TestCase("XOXXOXOXO", 'X', false)]
    public void IsWinningBoard_ShouldIdentifyCorrectly(string board, char symbol, bool expected)
    {
        var result = WinningLines.IsWinningBoard(board, symbol, out var winningLine);
        result.ShouldBe(expected);
        if (expected)
        {
            winningLine.ShouldNotBeNull();
        }
        else
        {
            winningLine.ShouldBeNull();
        }
    }
}
