namespace MauiUnit.Assertions;

/// <summary>
/// An MauiUnit assertion failed. A plain exception, so xUnit, NUnit and MSTest all report it as the
/// test's failure with its message; MauiUnit takes no dependency on any of them.
/// </summary>
public sealed class MauiUnitAssertionException(string message) : Exception(message);
