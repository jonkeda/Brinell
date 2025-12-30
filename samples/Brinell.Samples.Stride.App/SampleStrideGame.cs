using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Stride.CommunityToolkit.Bepu;

#if AUTOMATION_ENABLED
using Brinell.Stride.Automation;
#endif

namespace Brinell.Samples.Stride.App;

/// <summary>
/// Sample Stride game with testable UI.
/// </summary>
public class SampleStrideGame : Game
{
    private UIElement? _mainUI;
    private UIComponent? _uiComponent;
    private int _counter;
    private TextBlock? _counterDisplay;
    private EditText? _nameInput;
    private TextBlock? _greetingDisplay;
    private ToggleButton? _darkModeToggle;
    private Slider? _volumeSlider;
    private TextBlock? _volumeDisplay;
    private SpriteFont? _defaultFont;

    /// <summary>
    /// Main UI root element.
    /// </summary>
    public UIElement? MainUI => _mainUI;

    protected override void BeginRun()
    {
        base.BeginRun();

        // Try to load font, but continue without it for code-only samples
        TryLoadFont();

        // Set up a simple scene with UI
        SetupScene();
        CreateUI();

#if AUTOMATION_ENABLED
        // Enable automation for UI testing
        this.UseAutomation(() => _mainUI);
#endif
    }

    private void TryLoadFont()
    {
        try
        {
            _defaultFont = Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available - this is expected for code-only samples
            // UI will still work but text won't render
            _defaultFont = null;
        }
    }

    private void SetupScene()
    {
        // Use CommunityToolkit to set up scene with proper graphics compositor
        this.SetupBase3DScene();
        
        // Add UI component to the scene
        var uiEntity = new Entity("UI");
        _uiComponent = new UIComponent
        {
            Resolution = new Vector3(1280, 720, 1000),
            ResolutionStretch = ResolutionStretch.FixedWidthAdaptableHeight,
            IsFullScreen = true
        };
        uiEntity.Add(_uiComponent);
        SceneSystem.SceneInstance.RootScene.Entities.Add(uiEntity);
    }

    private void CreateUI()
    {
        // Create main panel
        var mainPanel = new StackPanel
        {
            Name = "MainPanel",
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 20, 20, 20)
        };

        // Title
        var title = new TextBlock
        {
            Name = "Title",
            Text = "Brinell Stride Sample",
            Font = _defaultFont,
            TextSize = 24,
            TextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 20)
        };
        mainPanel.Children.Add(title);

        // Counter section
        var counterSection = CreateCounterSection();
        mainPanel.Children.Add(counterSection);

        // Greeting section
        var greetingSection = CreateGreetingSection();
        mainPanel.Children.Add(greetingSection);

        // Settings section
        var settingsSection = CreateSettingsSection();
        mainPanel.Children.Add(settingsSection);

        _mainUI = mainPanel;

        // Attach UI to the UIComponent so it's part of the visual tree
        if (_uiComponent != null)
        {
            _uiComponent.Page = new UIPage { RootElement = _mainUI };
        }
    }

    private UIElement CreateCounterSection()
    {
        var panel = new StackPanel
        {
            Name = "CounterSection",
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // Counter display
        _counterDisplay = new TextBlock
        {
            Name = "CounterDisplay",
            Text = "Count: 0",
            Font = _defaultFont,
            TextSize = 18,
            TextColor = Color.White
        };
        panel.Children.Add(_counterDisplay);

        // Button row
        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var incrementButton = new Button
        {
            Name = "IncrementButton",
            Content = new TextBlock { Text = "Increment", Font = _defaultFont },
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 10, 0)
        };
        incrementButton.Click += (s, e) =>
        {
            _counter++;
            UpdateCounterDisplay();
        };
        buttonRow.Children.Add(incrementButton);

        var decrementButton = new Button
        {
            Name = "DecrementButton",
            Content = new TextBlock { Text = "Decrement", Font = _defaultFont },
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 10, 0)
        };
        decrementButton.Click += (s, e) =>
        {
            _counter--;
            UpdateCounterDisplay();
        };
        buttonRow.Children.Add(decrementButton);

        var resetButton = new Button
        {
            Name = "ResetButton",
            Content = new TextBlock { Text = "Reset", Font = _defaultFont },
            Padding = new Thickness(10, 5, 10, 5)
        };
        resetButton.Click += (s, e) =>
        {
            _counter = 0;
            UpdateCounterDisplay();
        };
        buttonRow.Children.Add(resetButton);

        panel.Children.Add(buttonRow);
        return panel;
    }

    private UIElement CreateGreetingSection()
    {
        var panel = new StackPanel
        {
            Name = "GreetingSection",
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var label = new TextBlock
        {
            Name = "NameLabel",
            Text = "Enter your name:",
            Font = _defaultFont,
            TextSize = 14,
            TextColor = Color.White
        };
        panel.Children.Add(label);

        _nameInput = new EditText
        {
            Name = "NameInput",
            Font = _defaultFont,
            TextSize = 16,
            MinimumWidth = 200,
            Margin = new Thickness(0, 5, 0, 5)
        };
        panel.Children.Add(_nameInput);

        var greetButton = new Button
        {
            Name = "GreetButton",
            Content = new TextBlock { Text = "Greet", Font = _defaultFont },
            Padding = new Thickness(10, 5, 10, 5)
        };
        greetButton.Click += (s, e) =>
        {
            var name = _nameInput?.Text ?? "World";
            if (string.IsNullOrWhiteSpace(name))
                name = "World";
            _greetingDisplay!.Text = $"Hello, {name}!";
        };
        panel.Children.Add(greetButton);

        _greetingDisplay = new TextBlock
        {
            Name = "GreetingDisplay",
            Text = "",
            Font = _defaultFont,
            TextSize = 18,
            TextColor = Color.LightGreen,
            Margin = new Thickness(0, 10, 0, 0)
        };
        panel.Children.Add(_greetingDisplay);

        return panel;
    }

    private UIElement CreateSettingsSection()
    {
        var panel = new StackPanel
        {
            Name = "SettingsSection",
            Orientation = Orientation.Vertical
        };

        var settingsLabel = new TextBlock
        {
            Name = "SettingsLabel",
            Text = "Settings",
            Font = _defaultFont,
            TextSize = 18,
            TextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(settingsLabel);

        // Dark mode toggle
        var darkModeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var darkModeLabel = new TextBlock
        {
            Name = "DarkModeLabel",
            Text = "Dark Mode: ",
            Font = _defaultFont,
            TextSize = 14,
            TextColor = Color.White
        };
        darkModeRow.Children.Add(darkModeLabel);

        _darkModeToggle = new ToggleButton
        {
            Name = "DarkModeToggle",
            Content = new TextBlock { Text = "Off", Font = _defaultFont },
            State = ToggleState.UnChecked
        };
        _darkModeToggle.Click += (s, e) =>
        {
            var textBlock = _darkModeToggle.Content as TextBlock;
            if (textBlock != null)
            {
                textBlock.Text = _darkModeToggle.State == ToggleState.Checked ? "On" : "Off";
            }
        };
        darkModeRow.Children.Add(_darkModeToggle);
        panel.Children.Add(darkModeRow);

        // Volume slider
        var volumeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var volumeLabel = new TextBlock
        {
            Name = "VolumeLabel",
            Text = "Volume: ",
            Font = _defaultFont,
            TextSize = 14,
            TextColor = Color.White
        };
        volumeRow.Children.Add(volumeLabel);

        _volumeSlider = new Slider
        {
            Name = "VolumeSlider",
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            MinimumWidth = 150
        };
        _volumeSlider.ValueChanged += (s, e) =>
        {
            _volumeDisplay!.Text = $"{_volumeSlider.Value:F0}%";
        };
        volumeRow.Children.Add(_volumeSlider);

        _volumeDisplay = new TextBlock
        {
            Name = "VolumeDisplay",
            Text = "50%",
            Font = _defaultFont,
            TextSize = 14,
            TextColor = Color.White,
            Margin = new Thickness(10, 0, 0, 0)
        };
        volumeRow.Children.Add(_volumeDisplay);

        panel.Children.Add(volumeRow);

        return panel;
    }

    private void UpdateCounterDisplay()
    {
        if (_counterDisplay != null)
        {
            _counterDisplay.Text = $"Count: {_counter}";
        }
    }
}
