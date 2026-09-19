namespace Brinell.Samples.Todo.UITests;

/// <summary>Timeouts for the Todo UI tests.</summary>
public static class TestConstants
{
    /// <summary>A whole test. xUnit enforces it only on tests returning <see cref="Task"/>.</summary>
    public const int DefaultTestTimeoutMs = 30_000;

    /// <summary>How long a page or a state may take to appear, including a sync against the fake backend.</summary>
    public const int PageTimeoutMs = 10_000;
}
