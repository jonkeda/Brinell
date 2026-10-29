using Microsoft.Maui.Controls;

namespace MauiHeadless.Assertions;

/// <summary>
/// The entry to MauiHeadless's assertions: <c>element.Should()</c>. Kept in its own namespace so a test
/// project that also uses another assertion library imports only the one it means.
/// </summary>
public static class ShouldExtensions
{
    /// <summary>Starts assertions on <paramref name="element"/>.</summary>
    public static ElementAssertions<T> Should<T>(this T element) where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        return new ElementAssertions<T>(element);
    }

    /// <summary>Asserts the switch is on.</summary>
    public static ElementAssertions<Switch> BeOn(this ElementAssertions<Switch> assertions)
        => assertions.Subject.IsToggled ? assertions : assertions.Fail("to be on, but it is off");

    /// <summary>Asserts the switch is off.</summary>
    public static ElementAssertions<Switch> BeOff(this ElementAssertions<Switch> assertions)
        => !assertions.Subject.IsToggled ? assertions : assertions.Fail("to be off, but it is on");

    /// <summary>Asserts the check box is checked.</summary>
    public static ElementAssertions<CheckBox> BeChecked(this ElementAssertions<CheckBox> assertions)
        => assertions.Subject.IsChecked ? assertions : assertions.Fail("to be checked, but it is not");

    /// <summary>Asserts the check box is not checked.</summary>
    public static ElementAssertions<CheckBox> NotBeChecked(this ElementAssertions<CheckBox> assertions)
        => !assertions.Subject.IsChecked ? assertions : assertions.Fail("not to be checked, but it is");

    /// <summary>Asserts the items view shows <paramref name="expected"/> rows.</summary>
    public static ElementAssertions<TView> HaveItemCount<TView>(this ElementAssertions<TView> assertions, int expected)
        where TView : ItemsView
    {
        var actual = assertions.Subject.ItemsSource?.Cast<object?>().Count() ?? 0;
        return actual == expected ? assertions : assertions.Fail($"to have {expected} items, but it has {actual}");
    }
}
