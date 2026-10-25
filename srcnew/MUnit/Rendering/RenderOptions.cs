namespace MUnit.Rendering;

/// <summary>How one element is rendered.</summary>
public sealed class RenderOptions
{
    /// <summary>Whether the page is told it appears (<c>OnAppearing</c>) once it is in its window. On by default, as on a device.</summary>
    public bool Appear { get; set; } = true;

    /// <summary>Whether the page is put in a <c>NavigationPage</c>, so it can push and pop.</summary>
    public bool InNavigationPage { get; set; }

    /// <summary>A binding context set on the element after it is made, before it appears.</summary>
    public object? BindingContext { get; set; }

    /// <summary>Puts the page in a <c>NavigationPage</c>.</summary>
    public RenderOptions WithNavigation()
    {
        InNavigationPage = true;
        return this;
    }

    /// <summary>Sets the binding context.</summary>
    public RenderOptions WithBindingContext(object bindingContext)
    {
        BindingContext = bindingContext;
        return this;
    }

    /// <summary>Renders without telling the page it appears.</summary>
    public RenderOptions WithoutAppearing()
    {
        Appear = false;
        return this;
    }
}
