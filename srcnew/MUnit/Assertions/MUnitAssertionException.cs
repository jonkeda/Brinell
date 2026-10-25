namespace MUnit.Assertions;

/// <summary>
/// An MUnit assertion failed. A plain exception, so xUnit, NUnit and MSTest all report it as the
/// test's failure with its message; MUnit takes no dependency on any of them.
/// </summary>
public sealed class MUnitAssertionException(string message) : Exception(message);
