namespace Brinell.Core.Exceptions;

/// <summary>
/// An error that waiting cannot change, raised by a driver below the call: the call ends at once
/// instead of polling to its budget (R0), as for <see cref="AppUnavailableException"/>. A driver marks
/// its own types with it, so Core needs no driver's types to know them.
/// </summary>
public interface IEndsCall
{
}
