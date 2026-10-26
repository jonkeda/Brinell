using Bruinet.Core.Exceptions;

namespace Bruinet.Maui.MauiUnit;

/// <summary>
/// A member that only a device can answer (layout, pixels, touch paths, the platform's own
/// dialogs), asked on the MauiUnit head: the test needs a device head, which it says rather than
/// passing wrongly. It ends the call at once (<see cref="IEndsCall"/>): no wait makes a device appear.
/// </summary>
public sealed class DeviceOnlyException : BruinetException, IEndsCall
{
    public DeviceOnlyException(string member)
        : base($"'{member}' needs a device: the MauiUnit head has no layout, pixels or platform input. Run this test on the Windows, Android or iOS head.")
    {
        Member = member;
    }

    /// <summary>The member that was asked.</summary>
    public string Member { get; }
}
