using Brinell.Maui.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Wrappers;

/// <summary>
/// Production wrapper that delegates all operations to the underlying AppiumDriver.
/// This class is a thin pass-through with minimal overhead.
/// </summary>
public sealed class MauiDriver : IMauiDriver
{
    private readonly AppiumDriver _driver;
    
    /// <summary>
    /// Creates a new MauiDriver wrapper.
    /// </summary>
    /// <param name="driver">The AppiumDriver to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when driver is null.</exception>
    public MauiDriver(AppiumDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }
    
    #region Element Finding
    
    /// <inheritdoc />
    public IMauiElement FindElement(By by) => new MauiElement(_driver.FindElement(by));
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(By by)
    {
        var elements = _driver.FindElements(by);
        return elements.Select(e => new MauiElement(e)).ToList();
    }
    
    #endregion
    
    #region Driver State
    
    /// <inheritdoc />
    public string PageSource => _driver.PageSource;
    
    /// <inheritdoc />
    public string CurrentWindowHandle => _driver.CurrentWindowHandle;
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> WindowHandles => _driver.WindowHandles;
    
    #endregion
    
    #region Session Management
    
    /// <inheritdoc />
    public void Quit() => _driver.Quit();
    
    /// <inheritdoc />
    public void Close() => _driver.Close();
    
    #endregion
    
    #region Screenshots
    
    /// <inheritdoc />
    public Screenshot GetScreenshot() => _driver.GetScreenshot();
    
    #endregion
    
    #region Context Switching
    
    /// <inheritdoc />
    public string Context
    {
        get => _driver.Context;
        set => _driver.Context = value;
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> Contexts => _driver.Contexts;
    
    #endregion
    
    #region Escape Hatch
    
    /// <inheritdoc />
    public AppiumDriver UnwrapDriver() => _driver;
    
    #endregion
}
