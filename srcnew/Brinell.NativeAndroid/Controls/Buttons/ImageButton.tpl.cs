using Brinell.Core.Locators;
using Brinell.NativeAndroid.Interfaces;

namespace Brinell.NativeAndroid.Controls.Buttons;

/// <summary>
/// A button that shows an image (<c>android.widget.ImageButton</c>); find it by its content description.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class ImageButton<TScope> : ClickableControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public ImageButton(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public ImageButton(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
