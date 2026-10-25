namespace Bruinell.Html.Bunit.Tests;

/// <summary>A service an app registers; the context finds it in <see cref="BunitTestContextOptions.FallbackServices"/>.</summary>
public interface IGreeting
{
    string Text { get; }
}

public sealed record FixedGreeting(string Text) : IGreeting;
