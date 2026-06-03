using System.Diagnostics;
using System.Reflection;
using Brinell.Maui.Enums;
using Brinell.Maui.Interfaces;

namespace Brinell.Maui;

/// <summary>
/// Factory for creating platform-appropriate MAUI drivers.
/// - Windows: Always uses FlaUI (native UI Automation)
/// - Android/iOS: Uses Appium
/// 
/// Driver implementations are loaded dynamically to avoid circular dependencies.
/// </summary>
public static class MauiDriverFactory
{
    private static Type? _appiumDriverType;
    private static Type? _flaUIDriverType;
    
    /// <summary>
    /// Creates a driver based on the specified options.
    /// </summary>
    /// <param name="options">Driver configuration options.</param>
    /// <returns>An IMauiDriver instance appropriate for the platform.</returns>
    /// <exception cref="ArgumentNullException">When options is null.</exception>
    /// <exception cref="ArgumentException">When required options are missing.</exception>
    /// <exception cref="PlatformNotSupportedException">When platform is not supported.</exception>
    public static IMauiDriver Create(MauiDriverOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        // Windows always uses FlaUI, mobile uses Appium
        return options.Platform switch
        {
            MauiPlatform.Windows => CreateFlaUIDriver(options),
            MauiPlatform.Android => CreateAppiumDriver(options),
            MauiPlatform.iOS => CreateAppiumDriver(options),
            _ => throw new ArgumentException($"Unsupported platform: {options.Platform}")
        };
    }
    
    private static IMauiDriver CreateFlaUIDriver(MauiDriverOptions options)
    {
        // Ensure we're on Windows
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "FlaUI driver is only available on Windows. " +
                "Use APPIUM_PLATFORM=android or APPIUM_PLATFORM=ios for other platforms.");
        }
        
        // Load FlaUI driver type dynamically
        var driverType = GetFlaUIDriverType();
        
        // Determine which constructor to use
        if (options.WindowHandle.HasValue)
        {
            var ctor = driverType.GetConstructor([typeof(IntPtr), typeof(WindowsInteractionOptions)])
                ?? throw new InvalidOperationException("FlaUIMauiDriver(IntPtr, WindowsInteractionOptions) constructor not found");
            return (IMauiDriver)ctor.Invoke([options.WindowHandle.Value, options.WindowsInteraction]);
        }
        else if (!string.IsNullOrEmpty(options.ProcessName))
        {
            var process = Process.GetProcessesByName(options.ProcessName).FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Process not found: {options.ProcessName}. " +
                    "Ensure the application is running or use AppPath to launch it.");
            
            var ctor = driverType.GetConstructor([typeof(Process), typeof(WindowsInteractionOptions)])
                ?? throw new InvalidOperationException("FlaUIMauiDriver(Process, WindowsInteractionOptions) constructor not found");
            return (IMauiDriver)ctor.Invoke([process, options.WindowsInteraction]);
        }
        else if (!string.IsNullOrEmpty(options.AppPath))
        {
            var ctor = driverType.GetConstructor([typeof(string), typeof(string), typeof(WindowsInteractionOptions)])
                ?? throw new InvalidOperationException("FlaUIMauiDriver(string, string, WindowsInteractionOptions) constructor not found");
            return (IMauiDriver)ctor.Invoke([options.AppPath, null, options.WindowsInteraction]);
        }
        else
        {
            throw new ArgumentException(
                "FlaUI driver requires AppPath, ProcessName, or WindowHandle. " +
                "Set APPIUM_APP_PATH environment variable or configure options.",
                nameof(options));
        }
    }
    
    private static IMauiDriver CreateAppiumDriver(MauiDriverOptions options)
    {
        ValidateAppiumOptions(options);
        
        // Load types dynamically from Appium assembly
        var appiumOptionsType = LoadAppiumOptionsType();
        var appiumDriverType = GetAppiumDriverType();
        
        // Build AppiumOptions using reflection
        var appiumOptions = BuildAppiumOptionsReflection(options, appiumOptionsType);
        
        // Create the platform-specific driver
        var rawDriver = CreatePlatformDriverReflection(options.AppiumServerUri, appiumOptions, options.Platform);
        
        // Create AppiumMauiDriver wrapper
        var ctor = appiumDriverType.GetConstructor([rawDriver.GetType(), typeof(MauiPlatform)])
            ?? appiumDriverType.GetConstructors().First();
        
        return (IMauiDriver)ctor.Invoke([rawDriver, options.Platform]);
    }
    
    private static void ValidateAppiumOptions(MauiDriverOptions options)
    {
        if (string.IsNullOrEmpty(options.AppPath))
        {
            throw new ArgumentException(
                "AppPath is required for Appium driver. " +
                "Set APPIUM_APP_PATH environment variable or options.AppPath.",
                nameof(options));
        }
    }
    
    private static Type GetFlaUIDriverType()
    {
        if (_flaUIDriverType != null) return _flaUIDriverType;
        
        try
        {
            var assembly = Assembly.Load("Brinell.Maui.FlaUI");
            _flaUIDriverType = assembly.GetType("Brinell.Maui.FlaUI.FlaUIMauiDriver")
                ?? throw new InvalidOperationException("FlaUIMauiDriver type not found in Brinell.Maui.FlaUI assembly");
            return _flaUIDriverType;
        }
        catch (FileNotFoundException)
        {
            throw new InvalidOperationException(
                "Brinell.Maui.FlaUI assembly not found. " +
                "Ensure the Brinell.Maui.FlaUI package is referenced in your project.");
        }
    }
    
    private static Type GetAppiumDriverType()
    {
        if (_appiumDriverType != null) return _appiumDriverType;
        
        try
        {
            var assembly = Assembly.Load("Brinell.Maui.Appium");
            _appiumDriverType = assembly.GetType("Brinell.Maui.Appium.AppiumMauiDriver")
                ?? throw new InvalidOperationException("AppiumMauiDriver type not found in Brinell.Maui.Appium assembly");
            return _appiumDriverType;
        }
        catch (FileNotFoundException)
        {
            throw new InvalidOperationException(
                "Brinell.Maui.Appium assembly not found. " +
                "Ensure the Brinell.Maui.Appium package is referenced in your project.");
        }
    }
    
    private static Type LoadAppiumOptionsType()
    {
        try
        {
            var assembly = Assembly.Load("Appium.WebDriver");
            return assembly.GetType("OpenQA.Selenium.Appium.AppiumOptions")
                ?? throw new InvalidOperationException("AppiumOptions type not found");
        }
        catch (FileNotFoundException)
        {
            throw new InvalidOperationException(
                "Appium.WebDriver assembly not found. " +
                "Ensure the Appium.WebDriver package is installed.");
        }
    }
    
    private static object BuildAppiumOptionsReflection(MauiDriverOptions options, Type appiumOptionsType)
    {
        var appiumOptions = Activator.CreateInstance(appiumOptionsType)!;
        
        // Set common properties using reflection
        var platformNameProp = appiumOptionsType.GetProperty("PlatformName");
        var automationNameProp = appiumOptionsType.GetProperty("AutomationName");
        var deviceNameProp = appiumOptionsType.GetProperty("DeviceName");
        var platformVersionProp = appiumOptionsType.GetProperty("PlatformVersion");
        var appProp = appiumOptionsType.GetProperty("App");
        
        switch (options.Platform)
        {
            case MauiPlatform.Android:
                platformNameProp?.SetValue(appiumOptions, "Android");
                automationNameProp?.SetValue(appiumOptions, "UiAutomator2");
                deviceNameProp?.SetValue(appiumOptions, options.DeviceName ?? "emulator-5554");
                appProp?.SetValue(appiumOptions, options.AppPath);
                break;
                
            case MauiPlatform.iOS:
                platformNameProp?.SetValue(appiumOptions, "iOS");
                automationNameProp?.SetValue(appiumOptions, "XCUITest");
                deviceNameProp?.SetValue(appiumOptions, options.DeviceName ?? "iPhone 15");
                platformVersionProp?.SetValue(appiumOptions, options.PlatformVersion ?? "17.0");
                appProp?.SetValue(appiumOptions, options.AppPath);
                break;
        }
        
        // Add additional capabilities
        var addCapMethod = appiumOptionsType.GetMethod("AddAdditionalAppiumOption");
        if (addCapMethod != null)
        {
            foreach (var cap in options.AdditionalCapabilities)
            {
                addCapMethod.Invoke(appiumOptions, [cap.Key, cap.Value]);
            }
        }
        
        return appiumOptions;
    }
    
    private static object CreatePlatformDriverReflection(Uri serverUri, object appiumOptions, MauiPlatform platform)
    {
        try
        {
            var assembly = Assembly.Load("Appium.WebDriver");
            
            Type driverType = platform switch
            {
                MauiPlatform.Android => assembly.GetType("OpenQA.Selenium.Appium.Android.AndroidDriver")!,
                MauiPlatform.iOS => assembly.GetType("OpenQA.Selenium.Appium.iOS.IOSDriver")!,
                _ => throw new ArgumentException($"Appium not supported for platform: {platform}")
            };
            
            var ctor = driverType.GetConstructor([typeof(Uri), appiumOptions.GetType()])
                ?? driverType.GetConstructors().First();
            
            return ctor.Invoke([serverUri, appiumOptions]);
        }
        catch (FileNotFoundException)
        {
            throw new InvalidOperationException(
                "Appium.WebDriver assembly not found. " +
                "Ensure the Appium.WebDriver package is installed.");
        }
    }
}
