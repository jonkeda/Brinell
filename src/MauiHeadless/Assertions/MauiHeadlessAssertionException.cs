namespace MauiHeadless.Assertions;

/// <summary>
/// An MauiHeadless assertion failed. A plain exception, so xUnit, NUnit and MSTest all report it as the
/// test's failure with its message; MauiHeadless takes no dependency on any of them.
/// </summary>
public sealed class MauiHeadlessAssertionException(string message) : Exception(message);
