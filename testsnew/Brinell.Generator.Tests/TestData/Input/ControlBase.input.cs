using Brinell.Core.Abstractions.Controls;

namespace Brinell.Maui.Controls;

public abstract class ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    #region Basic Interactions

    /// <summary>
    /// Sends keyboard keys to the control. Uses framework's Run for logging.
    /// Optimized to find element once and reuse.
    /// </summary>
    /// <param name="keys">The keys to send.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public virtual TScope SendKeys(string keys, int? timeoutMs = null)
    {
        return RunSetWithElement(keys, element =>
        {
            SendKeysCore(element, keys);
        }, timeoutMs);
    }
    
    /// <summary>
    /// Core implementation of SendKeys using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="keys">The keys to send.</param>
    protected virtual void SendKeysCore(IMauiElement element, string keys)
    {
        element.SendKeys(keys);
    }

    #endregion

    #region Visible

    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }

    #endregion

    #region Enabled

    protected virtual bool? IsEnabledCore(IMauiElement? element)
    {
        return element?.Enabled;
    }

    #endregion

    #region Exists

    protected virtual bool IsExistsCore(IMauiElement? element)
    {
        return element != null;
    }

    #endregion
}
