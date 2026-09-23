using Brinell.Core.Interfaces;

namespace Brinell.WinForms.Interfaces;

/// <summary>
/// Base scope for WinForms element finding operations.
/// </summary>
public interface IWinFormsElementScope : IElementScope<IWinFormsElement>
{
    /// <summary>
    /// Gets the test context.
    /// </summary>
    IWinFormsTestContext Context { get; }
}
