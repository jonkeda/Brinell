using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.NativeAndroid.Interfaces;

namespace Brinell.NativeAndroid.Controls.Text;

/// <summary>
/// Text shown to the user (<c>android.widget.TextView</c>): read and asserted, not edited.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class TextView<TScope> : ViewBase<TScope>, ITextControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public TextView(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public TextView(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
