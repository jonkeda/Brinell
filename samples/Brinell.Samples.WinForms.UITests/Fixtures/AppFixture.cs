using Brinell.Samples.WinForms.UITests.Pages;
using Brinell.FlaUI;
using Xunit;

namespace Brinell.Samples.WinForms.UITests.Fixtures;

/// <summary>
/// Shared application fixture for UI tests.
/// Launches the sample app once per test collection and reuses the same instance.
/// </summary>
public class AppFixture : IAsyncLifetime
{
    private FlaUIDriverAdapter? _driver;
    private FlaUITestContext? _context;
    private LoginPage? _loginPage;
    
    private const string AppPath = @"E:\repos\Private\Iosk\Oravey\Brinell\samples\Brinell.Samples.WinForms.App\bin\Debug\net9.0-windows\Brinell.Samples.WinForms.App.exe";

    /// <summary>
    /// The shared application driver.
    /// </summary>
    public FlaUIDriverAdapter Driver => _driver ?? throw new InvalidOperationException("Fixture not initialized");

    /// <summary>
    /// The shared test context.
    /// </summary>
    public FlaUITestContext Context => _context ?? throw new InvalidOperationException("Fixture not initialized");

    /// <summary>
    /// The shared login page object.
    /// </summary>
    public LoginPage LoginPage => _loginPage ?? throw new InvalidOperationException("Fixture not initialized");

    public async Task InitializeAsync()
    {
        try
        {
            _driver = new FlaUIDriverAdapter(AppPath);
            _context = new FlaUITestContext(_driver);
            _context.TestName = "UITestFixture";
            _loginPage = new LoginPage(_context);
        }
        catch (Exception ex)
        {
            _driver?.Dispose();
            _driver = null;
            throw new InvalidOperationException("Failed to initialize app fixture", ex);
        }
    }

    public async Task DisposeAsync()
    {
        _loginPage = null;
        _context = null;
        _driver?.Dispose();
        _driver = null;
        
        // Give the system time to clean up
        await Task.Delay(500);
    }
}

/// <summary>
/// xUnit collection definition for UI tests using shared app fixture.
/// </summary>
[CollectionDefinition("UI Tests Collection", DisableParallelization = true)]
public class UITestCollection : ICollectionFixture<AppFixture>
{
    // This class has no code, just exists to define the collection
}
