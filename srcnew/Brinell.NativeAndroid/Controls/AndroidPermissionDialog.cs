namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidPermissionDialog<TScope> : AndroidDialog<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    private static readonly Locator RootLocator = Locator.ByXPath(
        "//*[@resource-id='com.android.permissioncontroller:id/grant_dialog' or @resource-id='com.android.packageinstaller:id/permission_message']");

    public AndroidPermissionDialog(INativeAndroidScope<TScope> scope)
        : base(RootLocator, scope)
    {
    }

    public TScope Allow(int? timeoutMs = null)
        => TapFirstAvailable(timeoutMs,
            "com.android.permissioncontroller:id/permission_allow_button",
            "com.android.permissioncontroller:id/permission_allow_foreground_only_button",
            "com.android.permissioncontroller:id/permission_allow_one_time_button",
            "com.android.packageinstaller:id/permission_allow_button");

    public TScope Deny(int? timeoutMs = null)
        => TapFirstAvailable(timeoutMs,
            "com.android.permissioncontroller:id/permission_deny_button",
            "com.android.packageinstaller:id/permission_deny_button");

    private TScope TapFirstAvailable(int? timeoutMs, params string[] resourceIds)
    {
        foreach (var resourceId in resourceIds)
        {
            if (Context.Driver.TryFindElement(Locator.ById(resourceId), out var element, timeoutMs ?? 250))
            {
                element!.Click();
                return ContainingScope;
            }
        }

        throw new ElementNotFoundException($"No Android permission button was found. Tried: {string.Join(", ", resourceIds)}");
    }
}
