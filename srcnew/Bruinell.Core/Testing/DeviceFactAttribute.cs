using Xunit;

namespace Bruinell.Core.Testing;

/// <summary>
/// A MAUI test that needs a device (layout, pixels, a platform's own tree or driver): skipped on the
/// MUnit head with the reason, so the run still lists it (.my/mUnit). The scenario's counterpart is
/// <c>@device</c>; the web's is <see cref="BrowserFactAttribute"/>.
/// </summary>
public sealed class DeviceFactAttribute : FactAttribute
{
    /// <param name="reason">What the test needs the device for, shown in the skip.</param>
    public DeviceFactAttribute(string reason = "it needs a device")
    {
        if (MauiHeads.IsMunit)
        {
            Skip = $"Device only: {reason}; this run uses the MUnit head ({MauiHeads.Variable}=munit).";
        }
    }
}
