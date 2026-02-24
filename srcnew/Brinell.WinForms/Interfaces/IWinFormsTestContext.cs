using Brinell.Core.Interfaces;

namespace Brinell.WinForms.Interfaces;

/// <summary>
/// WinForms-specific test context interface.
/// </summary>
public interface IWinFormsTestContext : ITestContext<IWinFormsElement>, IWinFormsElementScope
{
    /// <summary>
    /// Gets the WinForms driver.
    /// </summary>
    IWinFormsDriver Driver { get; }
}
