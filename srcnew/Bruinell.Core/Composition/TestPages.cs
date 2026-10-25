namespace Bruinell.Core.Composition;

/// <summary>What counts as a page object when a suite's assemblies are scanned.</summary>
public static class TestPages
{
    /// <summary>The first model's page interface, in <c>Bruinell.Core.Legacy</c>, which Core does not reference (AD-029).</summary>
    public const string FirstModelPageInterface = "Bruinell.Core.Interfaces.IPageObject";

    /// <summary>Whether <paramref name="type"/> is a page of the first model (WPF, WinForms, Stride): it implements <c>IPageObject</c>.</summary>
    /// <param name="type">A type found in a suite's assembly.</param>
    public static bool IsFirstModelPage(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetInterface(FirstModelPageInterface) is not null;
    }
}
