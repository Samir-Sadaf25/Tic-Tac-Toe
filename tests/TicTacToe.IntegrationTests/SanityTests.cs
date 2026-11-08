using NUnit.Framework;
using Shouldly;

namespace TicTacToe.IntegrationTests;

[TestFixture]
public class SanityTests
{
    [Test]
    public void IntegrationTestFramework_IsWorking()
    {
        true.ShouldBeTrue();
    }
}
