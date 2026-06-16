using OpenQA.Selenium.Appium;

namespace Brinell.Maui.UITests.Tests;

/// <summary>
/// Diagnostic tests for debugging platform-specific UI structures.
/// </summary>
[Collection("Appium")]
public class DiagnosticTests
{
    private readonly MauiTestContext _context;

    public DiagnosticTests(AppiumFixture fixture)
    {
        _context = fixture.Context;
    }

    [Fact]
    public void Diagnostic_DumpPageSource_Android()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.True(true, "Android diagnostic test skipped on Windows");
            return;
        }

        // Get raw driver to access page source
        var driverProperty = typeof(MauiTestContext).GetProperty("Driver");
        var driver = driverProperty?.GetValue(_context);
        
        // Use reflection to get the underlying Appium driver
        var driverType = driver?.GetType();
        var rawDriverField = driverType?.GetField("_driver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (rawDriverField == null)
        {
            // Try the IMauiDriver wrapper approach - get the raw driver via PageSource
            Console.WriteLine("=== Getting page source via Driver wrapper ===");
        }
        
        // Access page source via the context's internal driver
        var contextType = typeof(MauiTestContext);
        var rawDriverFieldContext = contextType.GetField("_rawDriver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var appiumDriver = rawDriverFieldContext?.GetValue(_context) as AppiumDriver;
        
        if (appiumDriver != null)
        {
            Console.WriteLine("=== Android Page Source ===");
            var pageSource = appiumDriver.PageSource;
            
            // Print in chunks to avoid truncation
            const int chunkSize = 4000;
            for (int i = 0; i < pageSource.Length; i += chunkSize)
            {
                var chunk = pageSource.Substring(i, Math.Min(chunkSize, pageSource.Length - i));
                Console.WriteLine(chunk);
            }
            Console.WriteLine("=== End Page Source ===");
            
            // Try to find any elements with content-desc (AutomationId on Android)
            Console.WriteLine("\n=== Elements with content-desc ===");
            var elements = appiumDriver.FindElements(OpenQA.Selenium.By.XPath("//*[@content-desc]"));
            foreach (var el in elements.Take(50))
            {
                try
                {
                    var contentDesc = el.GetDomAttribute("content-desc") ?? el.GetAttribute("content-desc") ?? "(null)";
                    var className = el.GetDomAttribute("class") ?? el.GetAttribute("class") ?? "(null)";
                    Console.WriteLine($"  content-desc='{contentDesc}' class='{className}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error getting attributes: {ex.Message}");
                }
            }
            
            // Also try text-based search for "Basics"
            Console.WriteLine("\n=== Elements with text containing 'Basics' ===");
            try
            {
                var textElements = appiumDriver.FindElements(OpenQA.Selenium.By.XPath("//*[contains(@text, 'Basics') or contains(@content-desc, 'Basics')]"));
                foreach (var el in textElements.Take(10))
                {
                    var text = el.Text;
                    var className = el.GetDomAttribute("class") ?? "(null)";
                    Console.WriteLine($"  text='{text}' class='{className}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  XPath search failed: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Could not access underlying Appium driver");
        }
        
        Assert.True(true, "Diagnostic complete - check console output");
    }

    [Fact]
    public void Diagnostic_TestLocatorStrategies()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.True(true, "Android diagnostic test skipped on Windows");
            return;
        }

        // Access raw driver
        var contextType = typeof(MauiTestContext);
        var rawDriverFieldContext = contextType.GetField("_rawDriver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var appiumDriver = rawDriverFieldContext?.GetValue(_context) as AppiumDriver;
        
        if (appiumDriver == null)
        {
            Console.WriteLine("Could not access driver");
            Assert.Fail("No driver");
            return;
        }

        Console.WriteLine("=== Testing Locator Strategies for IncrementButton ===\n");

        // Strategy 1: By.Id with just the ID
        try
        {
            var el = appiumDriver.FindElement(OpenQA.Selenium.By.Id("IncrementButton"));
            Console.WriteLine($"✅ By.Id('IncrementButton') - FOUND: {el.TagName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ By.Id('IncrementButton') - NOT FOUND: {ex.GetType().Name}");
        }

        // Strategy 2: By.Id with full resource-id
        try
        {
            var el = appiumDriver.FindElement(OpenQA.Selenium.By.Id("com.brinell.samples.maui:id/IncrementButton"));
            Console.WriteLine($"✅ By.Id('com.brinell.samples.maui:id/IncrementButton') - FOUND: {el.TagName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ By.Id('com.brinell.samples.maui:id/IncrementButton') - NOT FOUND: {ex.GetType().Name}");
        }

        // Strategy 3: AccessibilityId
        try
        {
            var el = appiumDriver.FindElement(OpenQA.Selenium.Appium.MobileBy.AccessibilityId("IncrementButton"));
            Console.WriteLine($"✅ MobileBy.AccessibilityId('IncrementButton') - FOUND: {el.TagName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MobileBy.AccessibilityId('IncrementButton') - NOT FOUND: {ex.GetType().Name}");
        }

        // Strategy 4: XPath with resource-id
        try
        {
            var el = appiumDriver.FindElement(OpenQA.Selenium.By.XPath("//*[@resource-id='com.brinell.samples.maui:id/IncrementButton']"));
            Console.WriteLine($"✅ XPath resource-id full - FOUND: {el.TagName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ XPath resource-id full - NOT FOUND: {ex.GetType().Name}");
        }

        // Strategy 5: XPath with contains
        try
        {
            var el = appiumDriver.FindElement(OpenQA.Selenium.By.XPath("//*[contains(@resource-id, 'IncrementButton')]"));
            Console.WriteLine($"✅ XPath resource-id contains - FOUND: {el.TagName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ XPath resource-id contains - NOT FOUND: {ex.GetType().Name}");
        }

        // Strategy 6: UiAutomator selector
        try
        {
            var el = appiumDriver.FindElement(OpenQA.Selenium.Appium.MobileBy.AndroidUIAutomator("new UiSelector().resourceId(\"com.brinell.samples.maui:id/IncrementButton\")"));
            Console.WriteLine($"✅ AndroidUIAutomator full resourceId - FOUND: {el.TagName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AndroidUIAutomator full resourceId - NOT FOUND: {ex.GetType().Name}");
        }

        // Strategy 7: UiAutomator with resourceIdMatches
        try
        {
            var el = appiumDriver.FindElement(OpenQA.Selenium.Appium.MobileBy.AndroidUIAutomator("new UiSelector().resourceIdMatches(\".*IncrementButton\")"));
            Console.WriteLine($"✅ AndroidUIAutomator resourceIdMatches - FOUND: {el.TagName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AndroidUIAutomator resourceIdMatches - NOT FOUND: {ex.GetType().Name}");
        }

        Console.WriteLine("\n=== Test Complete ===");
        Assert.True(true);
    }

    [Fact]
    public void Diagnostic_TestButtonFind()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.True(true, "Android diagnostic test skipped on Windows");
            return;
        }

        // Directly test the Borderwork's button finding
        var mainPage = new Pages.MainPage(_context);
        
        Console.WriteLine("=== Testing Button via Borderwork ===\n");

        Console.WriteLine($"Platform: {_context.Platform}");
        
        // Test IncrementButton directly
        var button = mainPage.IncrementButton;
        
        Console.WriteLine($"Attempting IncrementButton.IsExists()...");
        try
        {
            var exists = button.IsExists();
            Console.WriteLine($"  IncrementButton.IsExists() = {exists}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  IncrementButton.IsExists() EXCEPTION: {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine("\n=== Test Complete ===");
        Assert.True(true);
    }
}
