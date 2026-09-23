namespace Brinell.Generator.Tests.Fixtures;

/// <summary>
/// Provides sample code snippets for unit testing the generator.
/// </summary>
public static class SampleCodeFixtures
{
    public const string SimpleClickableClass = @"namespace Brinell.Maui.Controls;

public abstract class SimpleClickable<TScope>
    where TScope : IScope<TScope>
{
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }
}";

    public const string MultiMethodClass = @"namespace Brinell.Maui.Controls;

public abstract class MultiMethod<TScope>
    where TScope : IScope<TScope>
{
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }

    protected virtual void HoverCore(IMauiElement element)
    {
        element.Hover();
    }

    public void NormalMethod() { }
}";

    public const string NoMethodsClass = @"namespace Brinell.Maui.Controls;

public class Empty
{
    public void SomeMethod() { }
}";

    public const string ProtectedNonVirtualMethod = @"namespace Brinell.Maui.Controls;

public abstract class NotVirtual
{
    protected void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }
}";

    public const string PublicCoreMethod = @"namespace Brinell.Maui.Controls;

public abstract class PublicCore
{
    public virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }
}";

    public const string MethodWithoutCoreSuffix = @"namespace Brinell.Maui.Controls;

public abstract class NoCore
{
    protected virtual void Click(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }
}";

    public const string MultipleParameters = @"namespace Brinell.Maui.Controls;

public abstract class ManyParams<TScope>
    where TScope : IScope<TScope>
{
    protected virtual void InteractCore(
        IMauiElement element,
        int? timeoutMs = null,
        string? context = null,
        bool force = false)
    {
        // Interaction logic
    }
}";

    public const string WithXmlDocumentation = @"namespace Brinell.Maui.Controls;

public abstract class WithDocs<TScope>
    where TScope : IScope<TScope>
{
    /// <summary>
    /// Clicks the element.
    /// </summary>
    /// <param name=""element"">The element to click.</param>
    /// <param name=""timeoutMs"">Optional timeout.</param>
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }
}";

    public const string MixedControlClass = @"namespace Brinell.Maui.Controls;

public abstract class MixedControl<TScope>
    where TScope : IScope<TScope>
{
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }

    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
}
