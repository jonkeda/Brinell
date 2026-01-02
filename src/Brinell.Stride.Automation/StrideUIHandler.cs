using Brinell.Stride.Communication;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;

namespace Brinell.Stride.Automation;

/// <summary>
/// Default handler for Stride UI automation commands.
/// </summary>
public class StrideUIHandler : IAutomationHandler
{
    private readonly Func<UIElement?> _rootProvider;
    private readonly Func<bool>? _isReadyProvider;
    private readonly Func<bool>? _isBusyProvider;
    private readonly IGame? _game;

    /// <summary>
    /// Create a handler with a UI root element provider.
    /// </summary>
    public StrideUIHandler(
        Func<UIElement?> rootProvider,
        Func<bool>? isReadyProvider = null,
        Func<bool>? isBusyProvider = null,
        IGame? game = null)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        _isReadyProvider = isReadyProvider;
        _isBusyProvider = isBusyProvider;
        _game = game;
    }

    /// <inheritdoc />
    public Task<AutomationResponse> HandleCommandAsync(AutomationCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = command.Type switch
            {
                "Query" => HandleQuery(command),
                "Action" => HandleAction(command),
                "GameQuery" => HandleGameQuery(command),
                _ => AutomationResponse.Fail($"Unknown command type: {command.Type}")
            };

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            return Task.FromResult(AutomationResponse.Fail($"Error: {ex.Message}"));
        }
    }

    private AutomationResponse HandleQuery(AutomationCommand command)
    {
        var target = command.Target ?? "";
        return command.Method switch
        {
            "GetState" => GetElementState(target),
            "Exists" => CheckExists(target),
            "IsVisible" => CheckVisible(target),
            "IsEnabled" => CheckEnabled(target),
            _ => AutomationResponse.Fail($"Unknown query method: {command.Method}")
        };
    }

    private AutomationResponse HandleAction(AutomationCommand command)
    {
        var target = command.Target ?? "";
        
        // Handle screenshot action (doesn't need a target element)
        if (command.Method == "TakeScreenshot")
        {
            var screenshotName = command.Args?.FirstOrDefault()?.ToString() ?? "screenshot";
            if (OperatingSystem.IsWindows())
            {
                return TakeScreenshot(screenshotName);
            }
            return AutomationResponse.Fail("Screenshot is only supported on Windows");
        }
        
        var element = FindElement(target);
        if (element == null)
        {
            return AutomationResponse.Fail($"NotFound:{target}:{command.Method}");
        }

        return command.Method switch
        {
            "Click" => PerformClick(element),
            "SetText" => SetText(element, command.Args?.FirstOrDefault()?.ToString() ?? ""),
            "Toggle" => PerformToggle(element),
            "SelectIndex" => SelectByIndex(element, Convert.ToInt32(command.Args?.FirstOrDefault() ?? 0)),
            "ScrollToIndex" => ScrollToIndex(element, Convert.ToInt32(command.Args?.FirstOrDefault() ?? 0)),
            _ => AutomationResponse.Fail($"Unknown action method: {command.Method}")
        };
    }

    private AutomationResponse HandleGameQuery(AutomationCommand command)
    {
        return command.Method switch
        {
            "IsReady" => AutomationResponse.Ok(_isReadyProvider?.Invoke() ?? true),
            "IsBusy" => AutomationResponse.Ok(_isBusyProvider?.Invoke() ?? false),
            "GetWindowInfo" => GetWindowInfo(),
            _ => AutomationResponse.Fail($"Unknown game query: {command.Method}")
        };
    }

    private AutomationResponse GetWindowInfo()
    {
        // Get window position using Windows API
        var windowInfo = new Dictionary<string, object>
        {
            ["windowX"] = 0,
            ["windowY"] = 0,
            ["windowWidth"] = 1280,
            ["windowHeight"] = 720,
            ["uiResolutionX"] = 1280,
            ["uiResolutionY"] = 720
        };

        if (_game?.Window != null)
        {
            var window = _game.Window;
            windowInfo["windowWidth"] = window.ClientBounds.Width;
            windowInfo["windowHeight"] = window.ClientBounds.Height;

            // Get actual window screen position using Windows API
            var hwnd = window.NativeWindow.Handle;
            if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out var rect))
            {
                // Client area offset from window position
                var clientPoint = new POINT { X = 0, Y = 0 };
                ClientToScreen(hwnd, ref clientPoint);
                
                windowInfo["windowX"] = clientPoint.X;
                windowInfo["windowY"] = clientPoint.Y;
            }
        }

        return AutomationResponse.Ok(windowInfo);
    }

    #region Windows API for window position
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }

    #endregion

    private AutomationResponse GetElementState(string automationId)
    {
        var element = FindElement(automationId);
        var state = CreateElementState(element, automationId);
        return AutomationResponse.Ok(state);
    }

    private AutomationResponse CheckExists(string automationId)
    {
        var element = FindElement(automationId);
        return AutomationResponse.Ok(element != null);
    }

    private AutomationResponse CheckVisible(string automationId)
    {
        var element = FindElement(automationId);
        return AutomationResponse.Ok(element?.IsVisible ?? false);
    }

    private AutomationResponse CheckEnabled(string automationId)
    {
        var element = FindElement(automationId);
        return AutomationResponse.Ok(element?.IsEnabled ?? false);
    }

    private AutomationResponse PerformClick(UIElement element)
    {
        // Stride doesn't have a direct "click" method - return success as actual click is done via input simulation
        return AutomationResponse.Ok(true);
    }

    private AutomationResponse SetText(UIElement element, string text)
    {
        if (element is EditText editText)
        {
            editText.Text = text;
            return AutomationResponse.Ok(true);
        }
        return AutomationResponse.Fail($"Element is not an EditText");
    }

    private AutomationResponse PerformToggle(UIElement element)
    {
        if (element is ToggleButton toggle)
        {
            toggle.State = toggle.State switch
            {
                ToggleState.UnChecked => ToggleState.Checked,
                ToggleState.Checked => ToggleState.UnChecked,
                ToggleState.Indeterminate => ToggleState.Checked,
                _ => ToggleState.Checked
            };
            return AutomationResponse.Ok(true);
        }
        return AutomationResponse.Fail($"Element is not a ToggleButton");
    }

    private AutomationResponse SelectByIndex(UIElement element, int index)
    {
        // For list-based controls
        if (element is Panel panel && panel.Children.Count > index)
        {
            // Simulate selection logic
            return AutomationResponse.Ok(true);
        }
        return AutomationResponse.Fail($"Cannot select index {index}");
    }

    private AutomationResponse ScrollToIndex(UIElement element, int index)
    {
        if (element is ScrollViewer scrollViewer)
        {
            // Calculate scroll position based on index
            return AutomationResponse.Ok(true);
        }
        return AutomationResponse.Fail($"Element is not scrollable");
    }

    private UIElement? FindElement(string automationId)
    {
        var root = _rootProvider();
        if (root == null)
            return null;

        return FindElementRecursive(root, automationId);
    }

    private UIElement? FindElementRecursive(UIElement element, string automationId)
    {
        if (element.Name == automationId)
            return element;

        if (element is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                var found = FindElementRecursive(child, automationId);
                if (found != null)
                    return found;
            }
        }
        else if (element is ContentControl contentControl && contentControl.Content is UIElement content)
        {
            var found = FindElementRecursive(content, automationId);
            if (found != null)
                return found;
        }
        else if (element is ScrollViewer scrollViewer && scrollViewer.Content is UIElement scrollContent)
        {
            var found = FindElementRecursive(scrollContent, automationId);
            if (found != null)
                return found;
        }

        return null;
    }

    private ElementState CreateElementState(UIElement? element, string automationId)
    {
        if (element == null)
        {
            return new ElementState
            {
                AutomationId = automationId,
                Exists = false
            };
        }

        var state = new ElementState
        {
            AutomationId = automationId,
            Exists = true,
            IsVisible = element.IsVisible,
            IsEnabled = element.IsEnabled,
            IsHitTestVisible = true, // Stride doesn't have this property directly
            IsFocused = false, // Would need to track focus state
            Bounds = GetElementBounds(element),
            Text = GetElementText(element),
            IsChecked = GetToggleState(element),
            Value = GetRangeValue(element),
            Minimum = GetRangeMinimum(element),
            Maximum = GetRangeMaximum(element),
            Items = GetItems(element),
            SelectedIndex = GetSelectedIndex(element),
            SelectedText = GetSelectedText(element)
        };

        return state;
    }

    private ElementBounds GetElementBounds(UIElement element)
    {
        // Get element's position from world matrix
        var worldMatrix = element.WorldMatrix;
        var renderSize = element.RenderSize;

        var width = renderSize.X;
        var height = renderSize.Y;

        // RenderSize often returns content size, not full control size
        // For buttons, this is just the text size, not including padding
        // Use minimum reasonable sizes for interactive controls
        var minWidth = 60f;  // Minimum clickable width
        var minHeight = 25f; // Minimum clickable height

        // Apply minimum sizes for interactive controls
        if (width < minWidth)
        {
            width = element.Width;
            if (float.IsNaN(width) || width < minWidth)
                width = element.MinimumWidth;
            if (float.IsNaN(width) || width < minWidth)
                width = minWidth;
        }

        // Apply minimum height
        if (height < minHeight)
        {
            height = element.Height;
            if (float.IsNaN(height) || height < minHeight)
                height = element.MinimumHeight;
            if (float.IsNaN(height) || height < minHeight)
                height = minHeight;
        }

        // Stride UI uses center-based coordinates
        // WorldMatrix.TranslationVector gives the element's CENTER position relative to UI center
        // UI center is at (resolution/2, resolution/2) = (640, 360) for 1280x720
        var uiCenterX = 640f;
        var uiCenterY = 360f;

        // Translation gives CENTER of element relative to UI center
        // Convert to top-left corner in screen coordinates
        var elementCenterX = uiCenterX + worldMatrix.TranslationVector.X;
        var elementCenterY = uiCenterY + worldMatrix.TranslationVector.Y;

        // Convert center to top-left
        var x = elementCenterX - width / 2;
        var y = elementCenterY - height / 2;

        return new ElementBounds
        {
            X = (int)x,
            Y = (int)y,
            Width = (int)width,
            Height = (int)height
        };
    }

    private string? GetElementText(UIElement element)
    {
        return element switch
        {
            TextBlock textBlock => textBlock.Text,
            EditText editText => editText.Text,
            Button button => (button.Content as TextBlock)?.Text,
            _ => null
        };
    }

    private bool? GetToggleState(UIElement element)
    {
        if (element is ToggleButton toggle)
        {
            return toggle.State == ToggleState.Checked;
        }
        return null;
    }

    private double? GetRangeValue(UIElement element)
    {
        if (element is Slider slider)
        {
            return slider.Value;
        }
        return null;
    }

    private double? GetRangeMinimum(UIElement element)
    {
        if (element is Slider slider)
        {
            return slider.Minimum;
        }
        return null;
    }

    private double? GetRangeMaximum(UIElement element)
    {
        if (element is Slider slider)
        {
            return slider.Maximum;
        }
        return null;
    }

    private List<string>? GetItems(UIElement element)
    {
        // Stride doesn't have a built-in ListBox, would need custom implementation
        return null;
    }

    private int GetSelectedIndex(UIElement element)
    {
        return -1;
    }

    private string? GetSelectedText(UIElement element)
    {
        return null;
    }

    [SupportedOSPlatform("windows")]
    private AutomationResponse TakeScreenshot(string screenshotName)
    {
        try
        {
            // Create screenshots directory if it doesn't exist
            var screenshotDir = Path.Combine(Path.GetTempPath(), "stride_screenshots");
            Directory.CreateDirectory(screenshotDir);

            // Take a screenshot using Windows screenshot API
            var screenshotPath = Path.Combine(screenshotDir, $"{screenshotName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            
            // Debug logging
            string debugInfo = $"Game={_game != null}, Window={_game?.Window != null}";
            
            // Use Windows screenshot functionality
            if (_game == null)
                return AutomationResponse.Fail($"Game instance not provided to handler [{debugInfo}]");
            
            if (_game.Window == null)
                return AutomationResponse.Fail($"Game window is null [{debugInfo}]");
                
            var hwnd = _game.Window.NativeWindow.Handle;
            debugInfo += $", Hwnd={hwnd:X}";
            if (hwnd == IntPtr.Zero)
                return AutomationResponse.Fail($"Invalid window handle (IntPtr.Zero) [{debugInfo}]");

            CaptureWindowScreenshot(hwnd, screenshotPath);
            return AutomationResponse.Ok(screenshotPath);
        }
        catch (Exception ex)
        {
            return AutomationResponse.Fail($"Failed to take screenshot: {ex.Message}");
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private void CaptureWindowScreenshot(IntPtr hwnd, string filePath)
    {
        // Get window dimensions
        if (!GetWindowRect(hwnd, out var rect))
            throw new InvalidOperationException("Failed to get window rect");

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        // Create device context for the window
        var hdcWindow = GetDC(hwnd);
        if (hdcWindow == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get device context");

        try
        {
            // Create compatible device context
            var hdcMemDC = CreateCompatibleDC(hdcWindow);
            if (hdcMemDC == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create compatible DC");

            try
            {
                // Create compatible bitmap
                var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);
                if (hBitmap == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to create compatible bitmap");

                try
                {
                    // Select bitmap into device context
                    var hOldBitmap = SelectObject(hdcMemDC, hBitmap);

                    // Copy pixels from window to memory DC
                    if (!BitBlt(hdcMemDC, 0, 0, width, height, hdcWindow, 0, 0, 0x00CC0020)) // SRCCOPY
                        throw new InvalidOperationException("BitBlt failed");

                    // Restore old bitmap
                    SelectObject(hdcMemDC, hOldBitmap);

                    // Convert bitmap to image and save
                    using (var bitmap = System.Drawing.Image.FromHbitmap(hBitmap))
                    {
                        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
            finally
            {
                DeleteDC(hdcMemDC);
            }
        }
        finally
        {
            ReleaseDC(hwnd, hdcWindow);
        }
    }

    #region Windows API for screenshots

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    #endregion
}
