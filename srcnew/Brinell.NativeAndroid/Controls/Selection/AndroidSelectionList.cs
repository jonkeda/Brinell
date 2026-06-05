namespace Brinell.NativeAndroid.Controls;

public class AndroidSelectionList<TScope> : AndroidSelectorControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidSelectionList(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidSelectionList(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public AndroidSelectionList(INativeAndroidScope<TScope> scope)
        : base(Locator.ByXPath("//*[@class='android.widget.ListView' or @class='androidx.recyclerview.widget.RecyclerView']"), scope)
    {
    }
}
