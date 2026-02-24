using Brinell.Core.Interfaces;

namespace Brinell.WinForms.Interfaces;

/// <summary>
/// WinForms page scope combining page object and element scope.
/// </summary>
/// <typeparam name="TSelf">The concrete page type (CRTP pattern).</typeparam>
public interface IWinFormsPage<TSelf> : IWinFormsScope<TSelf>, IPageObject<IWinFormsElement>
    where TSelf : IWinFormsPage<TSelf>
{
}
