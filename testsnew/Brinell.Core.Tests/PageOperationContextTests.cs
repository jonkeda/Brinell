using Brinell.Core.Interfaces;

namespace Brinell.Core.Tests;

public sealed class PageOperationContextTests
{
    [Fact]
    public void NestedContext_RestoresParentOnDispose()
    {
        var deadline = new OperationDeadline(TimeSpan.FromSeconds(1));
        using var outer = PageOperationContext.Begin(
            "Contacts",
            new ControlOperation("Read", ControlOperationKind.Get),
            deadline);
        var parent = PageOperationContext.Current;

        using (PageOperationContext.Begin(
                   "Contacts",
                   new ControlOperation("Nested", ControlOperationKind.Get),
                   deadline))
        {
            Assert.NotSame(parent, PageOperationContext.Current);
        }

        Assert.Same(parent, PageOperationContext.Current);
    }

    [Fact]
    public void OperationDeadline_UsesOneDecreasingBudget()
    {
        var deadline = new OperationDeadline(TimeSpan.FromSeconds(1));

        Assert.InRange(deadline.Remaining, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        Assert.False(deadline.IsExpired);
    }
}