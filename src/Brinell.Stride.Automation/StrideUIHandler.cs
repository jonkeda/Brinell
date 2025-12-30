using Brinell.Stride.Communication;
using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
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

    /// <summary>
    /// Create a handler with a UI root element provider.
    /// </summary>
    public StrideUIHandler(
        Func<UIElement?> rootProvider,
        Func<bool>? isReadyProvider = null,
        Func<bool>? isBusyProvider = null)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        _isReadyProvider = isReadyProvider;
        _isBusyProvider = isBusyProvider;
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
        var element = FindElement(target);
        if (element == null)
        {
            return AutomationResponse.Fail($"Element not found: {target}");
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
            _ => AutomationResponse.Fail($"Unknown game query: {command.Method}")
        };
    }

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
        // Get element's screen bounds
        var worldMatrix = element.WorldMatrix;
        var renderSize = element.RenderSize;

        // Fallback to explicit size if render size is zero (layout not yet computed)
        if (renderSize.X <= 0 && renderSize.Y <= 0)
        {
            // Try Width/Height properties
            var width = element.Width;
            var height = element.Height;

            // Fallback to minimum size if Width/Height are NaN
            if (float.IsNaN(width) || width <= 0)
                width = element.MinimumWidth;
            if (float.IsNaN(height) || height <= 0)
                height = element.MinimumHeight;

            // Default to a reasonable size if nothing is specified
            if (float.IsNaN(width) || width <= 0)
                width = 100;
            if (float.IsNaN(height) || height <= 0)
                height = 30;

            renderSize = new Vector3(width, height, 0);
        }

        return new ElementBounds
        {
            X = (int)worldMatrix.TranslationVector.X,
            Y = (int)worldMatrix.TranslationVector.Y,
            Width = (int)renderSize.X,
            Height = (int)renderSize.Y
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
}
