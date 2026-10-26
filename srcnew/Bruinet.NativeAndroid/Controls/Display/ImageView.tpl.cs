using Bruinet.Core.Locators;
using Bruinet.NativeAndroid.Interfaces;

namespace Bruinet.NativeAndroid.Controls.Display;

/// <summary>
/// An image (<c>android.widget.ImageView</c>); its content description says what it shows.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class ImageView<TScope> : ViewBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public ImageView(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public ImageView(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
