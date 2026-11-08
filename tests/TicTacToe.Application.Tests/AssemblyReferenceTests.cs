using NUnit.Framework;
using Shouldly;
using TicTacToe.Application;

namespace TicTacToe.Application.Tests;

[TestFixture]
public class AssemblyReferenceTests
{
    [Test]
    public void ApplicationAssemblyMarker_ResolvesApplicationAssembly()
    {
        typeof(AssemblyReference).Assembly.GetName().Name.ShouldBe("TicTacToe.Application");
    }
}
