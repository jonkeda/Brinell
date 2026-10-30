using Bruinet.Core.Locators;
using Bruinet.NativeAndroid.Interfaces;

namespace Bruinet.NativeAndroid.Controls.Container;

/// <summary>
/// A scroll view (<c>ScrollView</c>, <c>NestedScrollView</c>, <c>HorizontalScrollView</c>).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class ScrollView<TScope> : ScrollableControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public ScrollView(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public ScrollView(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
