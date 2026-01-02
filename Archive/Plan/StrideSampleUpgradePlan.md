# Stride Sample Upgrade Plan

**Version:** 1.0
**Date:** January 2, 2026
**Status:** Proposed

---

## Overview

Upgrade the `Brinell.Samples.Stride.App` from a simple gray screen with UI to an interactive game sample featuring:

1. A visible player character moving around a ground plane
2. WASD/Arrow key movement controls
3. ESC key to open a Settings overlay page
4. Settings page with comprehensive UI controls for testing

---

## Current State

The sample currently has:

- ✅ Basic 3D scene setup via `SetupBase3DScene()`
- ✅ UI system with buttons, text, sliders, toggles
- ✅ Automation integration for UI testing
- ❌ No visible game world (just gray/empty scene)
- ❌ No player character
- ❌ No keyboard input handling
- ❌ All UI is always visible (no menu/settings separation)

---

## Proposed Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        GAME STRUCTURE                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                           SampleStrideGame                             │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                     │  │
│  │  │     Game World      │  │    UI Overlay       │                     │  │
│  │  │  ┌───────────────┐  │  │  ┌───────────────┐  │                     │  │
│  │  │  │ Ground Plane  │  │  │  │  HUD (mini)   │  │                     │  │
│  │  │  │ (Green/Grass) │  │  │  │ - Position    │  │                     │  │
│  │  │  └───────────────┘  │  │  │ - ESC hint    │  │                     │  │
│  │  │  ┌───────────────┐  │  │  └───────────────┘  │                     │  │
│  │  │  │ Player Cube   │◄─┼──┼── WASD/Arrows       │                     │  │
│  │  │  │ (Colored)     │  │  │                     │                     │  │
│  │  │  └───────────────┘  │  │  ┌───────────────┐  │                     │  │
│  │  │  ┌───────────────┐  │  │  │ Settings Page │◄─┼── ESC to toggle     │  │
│  │  │  │ Camera        │  │  │  │ (Modal)       │  │                     │  │
│  │  │  │ (Top-down)    │  │  │  │ - All controls│  │                     │  │
│  │  │  └───────────────┘  │  │  └───────────────┘  │                     │  │
│  │  └─────────────────────┘  └─────────────────────┘                     │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Plan

### Phase 1: Game World Setup

#### Task 1.1: Create Ground Plane

Add a visible ground plane using CommunityToolkit primitives:

```csharp
private void SetupWorld()
{
    // Add ground plane (large flat green box)
    var ground = this.CreatePrimitive(PrimitiveModelType.Plane);
    ground.Name = "Ground";
    ground.Transform.Scale = new Vector3(20, 1, 20);
    ground.Transform.Position = new Vector3(0, 0, 0);
  
    // Apply green material
    var groundMaterial = this.CreateMaterial(Color.ForestGreen);
    ground.Get<ModelComponent>().Materials[0] = groundMaterial;
}
```

#### Task 1.2: Create Player Character

Add a simple colored cube as the player:

```csharp
private Entity? _player;
private Vector3 _playerPosition = new Vector3(0, 0.5f, 0);

private void CreatePlayer()
{
    _player = this.CreatePrimitive(PrimitiveModelType.Cube);
    _player.Name = "Player";
    _player.Transform.Scale = new Vector3(0.5f, 1f, 0.5f);
    _player.Transform.Position = _playerPosition;
  
    // Apply player color (blue)
    var playerMaterial = this.CreateMaterial(Color.CornflowerBlue);
    _player.Get<ModelComponent>().Materials[0] = playerMaterial;
}
```

#### Task 1.3: Setup Camera

Position camera for top-down/isometric view:

```csharp
private void SetupCamera()
{
    // CommunityToolkit's SetupBase3DScene creates a camera
    // Adjust it for top-down view
    var cameraEntity = SceneSystem.SceneInstance.RootScene.Entities
        .FirstOrDefault(e => e.Get<CameraComponent>() != null);
  
    if (cameraEntity != null)
    {
        // Position above and looking down at an angle (isometric-like)
        cameraEntity.Transform.Position = new Vector3(0, 10, 8);
        cameraEntity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-50));
    }
}
```

### Phase 2: Player Movement

#### Task 2.1: Add Input Script

Create a simple player controller:

```csharp
private float _moveSpeed = 5f;

protected override void Update(GameTime gameTime)
{
    base.Update(gameTime);
  
    if (_settingsOpen) return; // Don't move when settings are open
  
    HandlePlayerInput((float)gameTime.Elapsed.TotalSeconds);
}

private void HandlePlayerInput(float deltaTime)
{
    if (_player == null) return;
  
    var input = Input;
    var movement = Vector3.Zero;
  
    // WASD or Arrow keys
    if (input.IsKeyDown(Keys.W) || input.IsKeyDown(Keys.Up))
        movement.Z -= 1;
    if (input.IsKeyDown(Keys.S) || input.IsKeyDown(Keys.Down))
        movement.Z += 1;
    if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.Left))
        movement.X -= 1;
    if (input.IsKeyDown(Keys.D) || input.IsKeyDown(Keys.Right))
        movement.X += 1;
  
    if (movement != Vector3.Zero)
    {
        movement.Normalize();
        _playerPosition += movement * _moveSpeed * deltaTime;
      
        // Clamp to ground bounds
        _playerPosition.X = Math.Clamp(_playerPosition.X, -9f, 9f);
        _playerPosition.Z = Math.Clamp(_playerPosition.Z, -9f, 9f);
      
        _player.Transform.Position = _playerPosition;
        UpdatePositionDisplay();
    }
}
```

#### Task 2.2: Add Position Display to HUD

Show player position in the UI:

```csharp
private TextBlock? _positionDisplay;

// In CreateHUD():
_positionDisplay = new TextBlock
{
    Name = "PositionDisplay",
    Text = "Position: (0.0, 0.0)",
    Font = _font,
    TextSize = 12,
    TextColor = Color.White
};

private void UpdatePositionDisplay()
{
    if (_positionDisplay != null)
    {
        _positionDisplay.Text = $"Position: ({_playerPosition.X:F1}, {_playerPosition.Z:F1})";
    }
}
```

### Phase 3: Settings Page Toggle

#### Task 3.1: Handle ESC Key

Toggle settings overlay with ESC:

```csharp
private bool _settingsOpen = false;
private UIElement? _settingsPanel;
private UIElement? _hudPanel;

protected override void Update(GameTime gameTime)
{
    base.Update(gameTime);
  
    // Toggle settings with ESC
    if (Input.IsKeyPressed(Keys.Escape))
    {
        ToggleSettings();
    }
  
    if (!_settingsOpen)
    {
        HandlePlayerInput((float)gameTime.Elapsed.TotalSeconds);
    }
}

private void ToggleSettings()
{
    _settingsOpen = !_settingsOpen;
  
    if (_settingsPanel != null)
    {
        _settingsPanel.Visibility = _settingsOpen 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }
  
    // Update HUD hint
    if (_escHint != null)
    {
        _escHint.Text = _settingsOpen 
            ? "Press ESC to close" 
            : "Press ESC for Settings";
    }
}
```

#### Task 3.2: Create HUD (Always Visible)

Minimal HUD showing game state:

```csharp
private UIElement CreateHUD()
{
    var hud = new StackPanel
    {
        Name = "HUD",
        Orientation = Orientation.Vertical,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Margin = new Thickness(20, 20, 0, 0)
    };
  
    // Title
    var title = new TextBlock
    {
        Name = "GameTitle",
        Text = "Brinell Stride Sample",
        Font = _font,
        TextSize = 18,
        TextColor = Color.White
    };
    hud.Children.Add(title);
  
    // Position display
    _positionDisplay = new TextBlock
    {
        Name = "PositionDisplay",
        Text = "Position: (0.0, 0.0)",
        Font = _font,
        TextSize = 12,
        TextColor = Color.LightGray
    };
    hud.Children.Add(_positionDisplay);
  
    // ESC hint
    _escHint = new TextBlock
    {
        Name = "EscHint",
        Text = "Press ESC for Settings",
        Font = _font,
        TextSize = 11,
        TextColor = Color.Yellow,
        Margin = new Thickness(0, 10, 0, 0)
    };
    hud.Children.Add(_escHint);
  
    // Movement hint
    var moveHint = new TextBlock
    {
        Name = "MovementHint",
        Text = "WASD or Arrow Keys to move",
        Font = _font,
        TextSize = 11,
        TextColor = Color.Gray
    };
    hud.Children.Add(moveHint);
  
    return hud;
}
```

### Phase 4: Comprehensive Settings Page

#### Task 4.1: Settings Panel Structure

Create a comprehensive settings page with all testable controls:

```csharp
private UIElement CreateSettingsPanel()
{
    // Dark semi-transparent background overlay
    var overlay = new Grid
    {
        Name = "SettingsOverlay",
        BackgroundColor = new Color(0, 0, 0, 180),
        Visibility = Visibility.Collapsed
    };
  
    // Settings content panel (centered)
    var content = new StackPanel
    {
        Name = "SettingsPanel",
        Orientation = Orientation.Vertical,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        BackgroundColor = new Color(50, 50, 50, 255),
        Margin = new Thickness(50)
    };
  
    // Header
    content.Children.Add(CreateSettingsHeader());
  
    // Audio Settings Section
    content.Children.Add(CreateAudioSection());
  
    // Graphics Settings Section  
    content.Children.Add(CreateGraphicsSection());
  
    // Gameplay Settings Section
    content.Children.Add(CreateGameplaySection());
  
    // Buttons Section
    content.Children.Add(CreateButtonsSection());
  
    overlay.Children.Add(content);
    return overlay;
}
```

#### Task 4.2: Audio Settings Section

```csharp
private UIElement CreateAudioSection()
{
    var panel = new StackPanel
    {
        Name = "AudioSection",
        Orientation = Orientation.Vertical,
        Margin = new Thickness(0, 0, 0, 20)
    };
  
    // Section header
    panel.Children.Add(new TextBlock
    {
        Name = "AudioHeader",
        Text = "🔊 Audio Settings",
        Font = _font,
        TextSize = 16,
        TextColor = Color.White
    });
  
    // Master Volume slider
    var masterRow = CreateSliderRow("MasterVolume", "Master Volume", 0, 100, 80);
    panel.Children.Add(masterRow);
  
    // Music Volume slider
    var musicRow = CreateSliderRow("MusicVolume", "Music Volume", 0, 100, 60);
    panel.Children.Add(musicRow);
  
    // SFX Volume slider
    var sfxRow = CreateSliderRow("SFXVolume", "SFX Volume", 0, 100, 70);
    panel.Children.Add(sfxRow);
  
    // Mute toggle
    var muteRow = CreateToggleRow("MuteAudio", "Mute All Audio");
    panel.Children.Add(muteRow);
  
    return panel;
}
```

#### Task 4.3: Graphics Settings Section

```csharp
private UIElement CreateGraphicsSection()
{
    var panel = new StackPanel
    {
        Name = "GraphicsSection",
        Orientation = Orientation.Vertical,
        Margin = new Thickness(0, 0, 0, 20)
    };
  
    panel.Children.Add(new TextBlock
    {
        Name = "GraphicsHeader",
        Text = "🖥️ Graphics Settings",
        Font = _font,
        TextSize = 16,
        TextColor = Color.White
    });
  
    // Fullscreen toggle
    panel.Children.Add(CreateToggleRow("Fullscreen", "Fullscreen Mode"));
  
    // VSync toggle
    panel.Children.Add(CreateToggleRow("VSync", "VSync"));
  
    // Brightness slider
    panel.Children.Add(CreateSliderRow("Brightness", "Brightness", 0, 100, 50));
  
    // Quality dropdown (using ListBox for testability)
    panel.Children.Add(CreateListBoxRow("QualityLevel", "Quality", 
        new[] { "Low", "Medium", "High", "Ultra" }, 2));
  
    return panel;
}
```

#### Task 4.4: Gameplay Settings Section

```csharp
private UIElement CreateGameplaySection()
{
    var panel = new StackPanel
    {
        Name = "GameplaySection",
        Orientation = Orientation.Vertical,
        Margin = new Thickness(0, 0, 0, 20)
    };
  
    panel.Children.Add(new TextBlock
    {
        Name = "GameplayHeader",
        Text = "🎮 Gameplay Settings",
        Font = _font,
        TextSize = 16,
        TextColor = Color.White
    });
  
    // Player name input
    panel.Children.Add(CreateTextInputRow("PlayerName", "Player Name", "Player"));
  
    // Movement speed slider
    panel.Children.Add(CreateSliderRow("MoveSpeed", "Move Speed", 1, 10, 5));
  
    // Camera sensitivity slider
    panel.Children.Add(CreateSliderRow("Sensitivity", "Camera Sensitivity", 1, 10, 5));
  
    // Invert Y toggle
    panel.Children.Add(CreateToggleRow("InvertY", "Invert Y Axis"));
  
    // Show FPS toggle
    panel.Children.Add(CreateToggleRow("ShowFPS", "Show FPS Counter"));
  
    // Difficulty dropdown
    panel.Children.Add(CreateListBoxRow("Difficulty", "Difficulty",
        new[] { "Easy", "Normal", "Hard", "Nightmare" }, 1));
  
    return panel;
}
```

#### Task 4.5: Buttons Section

```csharp
private UIElement CreateButtonsSection()
{
    var panel = new StackPanel
    {
        Name = "ButtonsSection",
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 20, 0, 0)
    };
  
    // Apply button
    var applyButton = new Button
    {
        Name = "ApplyButton",
        Content = new TextBlock { Text = "Apply", Font = _font },
        Padding = new Thickness(20, 10, 20, 10),
        Margin = new Thickness(0, 0, 10, 0)
    };
    applyButton.Click += (s, e) => ApplySettings();
    panel.Children.Add(applyButton);
  
    // Reset button
    var resetButton = new Button
    {
        Name = "ResetButton",
        Content = new TextBlock { Text = "Reset to Defaults", Font = _font },
        Padding = new Thickness(20, 10, 20, 10),
        Margin = new Thickness(0, 0, 10, 0)
    };
    resetButton.Click += (s, e) => ResetSettings();
    panel.Children.Add(resetButton);
  
    // Close button
    var closeButton = new Button
    {
        Name = "CloseButton",
        Content = new TextBlock { Text = "Close (ESC)", Font = _font },
        Padding = new Thickness(20, 10, 20, 10)
    };
    closeButton.Click += (s, e) => ToggleSettings();
    panel.Children.Add(closeButton);
  
    return panel;
}
```

#### Task 4.6: Helper Methods for Control Creation

```csharp
private UIElement CreateSliderRow(string name, string label, float min, float max, float initial)
{
    var row = new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 5, 0, 5)
    };
  
    row.Children.Add(new TextBlock
    {
        Name = $"{name}Label",
        Text = $"{label}: ",
        Font = _font,
        TextSize = 12,
        TextColor = Color.White,
        MinimumWidth = 150
    });
  
    var slider = new Slider
    {
        Name = $"{name}Slider",
        Minimum = min,
        Maximum = max,
        Value = initial,
        MinimumWidth = 150
    };
    row.Children.Add(slider);
  
    var display = new TextBlock
    {
        Name = $"{name}Display",
        Text = $"{initial:F0}",
        Font = _font,
        TextSize = 12,
        TextColor = Color.White,
        Margin = new Thickness(10, 0, 0, 0),
        MinimumWidth = 40
    };
    slider.ValueChanged += (s, e) => display.Text = $"{slider.Value:F0}";
    row.Children.Add(display);
  
    return row;
}

private UIElement CreateToggleRow(string name, string label)
{
    var row = new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 5, 0, 5)
    };
  
    row.Children.Add(new TextBlock
    {
        Name = $"{name}Label",
        Text = $"{label}: ",
        Font = _font,
        TextSize = 12,
        TextColor = Color.White,
        MinimumWidth = 150
    });
  
    var toggle = new ToggleButton
    {
        Name = $"{name}Toggle",
        Content = new TextBlock { Text = "Off", Font = _font },
        State = ToggleState.UnChecked
    };
    toggle.Click += (s, e) =>
    {
        var text = toggle.Content as TextBlock;
        if (text != null)
            text.Text = toggle.State == ToggleState.Checked ? "On" : "Off";
    };
    row.Children.Add(toggle);
  
    return row;
}

private UIElement CreateTextInputRow(string name, string label, string placeholder)
{
    var row = new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 5, 0, 5)
    };
  
    row.Children.Add(new TextBlock
    {
        Name = $"{name}Label",
        Text = $"{label}: ",
        Font = _font,
        TextSize = 12,
        TextColor = Color.White,
        MinimumWidth = 150
    });
  
    var input = new EditText
    {
        Name = $"{name}Input",
        Font = _font,
        TextSize = 12,
        MinimumWidth = 200,
        Text = placeholder
    };
    row.Children.Add(input);
  
    return row;
}

private UIElement CreateListBoxRow(string name, string label, string[] items, int selectedIndex)
{
    var row = new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 5, 0, 5)
    };
  
    row.Children.Add(new TextBlock
    {
        Name = $"{name}Label",
        Text = $"{label}: ",
        Font = _font,
        TextSize = 12,
        TextColor = Color.White,
        MinimumWidth = 150
    });
  
    // Using horizontal button group as dropdown alternative
    var buttonGroup = new StackPanel
    {
        Name = $"{name}Options",
        Orientation = Orientation.Horizontal
    };
  
    for (int i = 0; i < items.Length; i++)
    {
        var index = i;
        var btn = new ToggleButton
        {
            Name = $"{name}Option{i}",
            Content = new TextBlock { Text = items[i], Font = _font },
            State = i == selectedIndex ? ToggleState.Checked : ToggleState.UnChecked,
            Margin = new Thickness(0, 0, 5, 0)
        };
        buttonGroup.Children.Add(btn);
    }
    row.Children.Add(buttonGroup);
  
    return row;
}
```

---

## File Changes Summary

### Modified Files

| File                                  | Changes                                                             |
| ------------------------------------- | ------------------------------------------------------------------- |
| `SampleStrideGame.cs`               | Complete rewrite - add world, player, input handling, settings page |
| `Brinell.Samples.Stride.App.csproj` | Add Stride.Input package reference if needed                        |

### New Files (Optional - for organization)

| File                    | Purpose                                                        |
| ----------------------- | -------------------------------------------------------------- |
| `PlayerController.cs` | Player movement logic (optional - can stay in main class)      |
| `SettingsUI.cs`       | Settings panel UI creation (optional - can stay in main class) |

---

## Page Objects to Update

### Existing: MainPage.cs → GamePage.cs

Rename and update for HUD controls:

```csharp
public class GamePage : StridePageBase
{
    public override string Name => "Game Page";
  
    // HUD Controls
    public StrideTextBlockControl GameTitle => TextBlock("GameTitle");
    public StrideTextBlockControl PositionDisplay => TextBlock("PositionDisplay");
    public StrideTextBlockControl EscHint => TextBlock("EscHint");
    public StrideTextBlockControl MovementHint => TextBlock("MovementHint");
  
    // Actions
    public void OpenSettings() => PressKey(VirtualKey.Escape);
}
```

### New: SettingsPage.cs

```csharp
public class SettingsPage : StridePageBase
{
    public override string Name => "Settings Page";
  
    public SettingsPage(StrideTestContext context) : base(context, "SettingsPanel")
    {
    }
  
    // Audio Controls
    public StrideSliderControl MasterVolumeSlider => Slider("MasterVolumeSlider");
    public StrideSliderControl MusicVolumeSlider => Slider("MusicVolumeSlider");
    public StrideSliderControl SFXVolumeSlider => Slider("SFXVolumeSlider");
    public StrideToggleButtonControl MuteToggle => ToggleButton("MuteAudioToggle");
  
    // Graphics Controls
    public StrideToggleButtonControl FullscreenToggle => ToggleButton("FullscreenToggle");
    public StrideToggleButtonControl VSyncToggle => ToggleButton("VSyncToggle");
    public StrideSliderControl BrightnessSlider => Slider("BrightnessSlider");
  
    // Gameplay Controls
    public StrideEditTextControl PlayerNameInput => EditText("PlayerNameInput");
    public StrideSliderControl MoveSpeedSlider => Slider("MoveSpeedSlider");
    public StrideSliderControl SensitivitySlider => Slider("SensitivitySlider");
    public StrideToggleButtonControl InvertYToggle => ToggleButton("InvertYToggle");
    public StrideToggleButtonControl ShowFPSToggle => ToggleButton("ShowFPSToggle");
  
    // Buttons
    public StrideButtonControl ApplyButton => Button("ApplyButton");
    public StrideButtonControl ResetButton => Button("ResetButton");
    public StrideButtonControl CloseButton => Button("CloseButton");
  
    // Actions
    public void Close() => PressKey(VirtualKey.Escape);
  
    public void SetMasterVolume(double value) => MasterVolumeSlider.SetValue(value);
    public void SetMusicVolume(double value) => MusicVolumeSlider.SetValue(value);
    public void SetPlayerName(string name) => PlayerNameInput.SetText(name);
}
```

---

## Test Cases to Add

### GameplayTests.cs (New)

```csharp
[Fact]
public void Player_InitialPosition_IsAtOrigin()
{
    var game = new GamePage(Context);
    game.CheckActive();
  
    game.PositionDisplay.AssertTextContains("0.0");
}

[Fact]
public void Player_MoveWithWASD_UpdatesPosition()
{
    var game = new GamePage(Context);
    game.CheckActive();
  
    // Hold W for 500ms
    Context.HoldKey(VirtualKey.W, 500);
  
    // Position should have changed
    var posText = game.PositionDisplay.GetText();
    posText.Should().NotContain("0.0, 0.0");
}

[Fact]
public void Game_PressEscape_OpensSettings()
{
    var game = new GamePage(Context);
    game.CheckActive();
  
    game.OpenSettings();
  
    var settings = new SettingsPage(Context);
    settings.CheckActive();
}
```

### SettingsTests.cs (Expanded)

```csharp
[Fact]
public void Settings_OpenAndClose_ReturnsToGame()
{
    var game = new GamePage(Context);
    game.CheckActive();
  
    game.OpenSettings();
    var settings = new SettingsPage(Context);
    settings.CheckActive();
  
    settings.Close();
    game.CheckActive();
}

[Fact]
public void Settings_AllSliders_AreInteractable()
{
    var game = new GamePage(Context);
    game.OpenSettings();
    var settings = new SettingsPage(Context);
  
    settings.MasterVolumeSlider.AssertExists();
    settings.MusicVolumeSlider.AssertExists();
    settings.SFXVolumeSlider.AssertExists();
    settings.BrightnessSlider.AssertExists();
    settings.MoveSpeedSlider.AssertExists();
    settings.SensitivitySlider.AssertExists();
}

[Fact]
public void Settings_AllToggles_CanBeToggled()
{
    var game = new GamePage(Context);
    game.OpenSettings();
    var settings = new SettingsPage(Context);
  
    settings.MuteToggle.Toggle();
    settings.MuteToggle.AssertChecked();
  
    settings.FullscreenToggle.Toggle();
    settings.FullscreenToggle.AssertChecked();
  
    settings.VSyncToggle.Toggle();
    settings.VSyncToggle.AssertChecked();
}

[Fact]
public void Settings_PlayerName_CanBeChanged()
{
    var game = new GamePage(Context);
    game.OpenSettings();
    var settings = new SettingsPage(Context);
  
    settings.SetPlayerName("TestPlayer");
    settings.PlayerNameInput.AssertTextEquals("TestPlayer");
}

[Fact]
public void Settings_ApplyButton_IsClickable()
{
    var game = new GamePage(Context);
    game.OpenSettings();
    var settings = new SettingsPage(Context);
  
    settings.ApplyButton.AssertEnabled();
    settings.ApplyButton.Click();
    // Should remain on settings (no crash)
    settings.CheckActive();
}
```

---

## Controls Available for Testing

| Control Type           | Count | Examples                                                      |
| ---------------------- | ----- | ------------------------------------------------------------- |
| **Button**       | 3     | Apply, Reset, Close                                           |
| **Slider**       | 7     | Master/Music/SFX Volume, Brightness, Move Speed, Sensitivity  |
| **ToggleButton** | 7     | Mute, Fullscreen, VSync, InvertY, ShowFPS, Difficulty options |
| **EditText**     | 1     | Player Name                                                   |
| **TextBlock**    | 10+   | Labels, displays, headers                                     |
| **Panel**        | 5+    | Sections, rows                                                |

---

## Execution Order

```
┌─────────────────────────────────────────────────────────────────┐
│ Phase 1: Game World (Week 1)                                    │
├─────────────────────────────────────────────────────────────────┤
│ 1. Add ground plane with green material                         │
│ 2. Create player cube with blue material                        │
│ 3. Adjust camera to top-down/isometric view                     │
│ 4. Verify visual display works                                  │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Phase 2: Player Movement (Week 1)                               │
├─────────────────────────────────────────────────────────────────┤
│ 1. Add Update loop with input handling                          │
│ 2. Implement WASD/Arrow movement                                │
│ 3. Add position display in HUD                                  │
│ 4. Test movement manually                                       │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Phase 3: Settings Toggle (Week 1)                               │
├─────────────────────────────────────────────────────────────────┤
│ 1. Handle ESC key press                                         │
│ 2. Create HUD with ESC hint                                     │
│ 3. Create settings overlay structure                            │
│ 4. Toggle visibility on ESC                                     │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Phase 4: Settings UI (Week 2)                                   │
├─────────────────────────────────────────────────────────────────┤
│ 1. Create Audio settings section                                │
│ 2. Create Graphics settings section                             │
│ 3. Create Gameplay settings section                             │
│ 4. Create Buttons section                                       │
│ 5. Wire up interactions                                         │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Phase 5: Page Objects & Tests (Week 2)                          │
├─────────────────────────────────────────────────────────────────┤
│ 1. Update MainPage → GamePage                                   │
│ 2. Create SettingsPage                                          │
│ 3. Add GameplayTests                                            │
│ 4. Expand SettingsTests                                         │
│ 5. Verify all tests pass                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## Dependencies

- `Stride.CommunityToolkit.Windows` - Already referenced (for primitives, materials)
- `Stride.CommunityToolkit.Bepu` - Already referenced (for physics if needed)
- `Stride.Input` - Included with Stride.Engine

No new package references required.

---

## Risks & Mitigationsdd

| Risk                                     | Impact | Mitigation                                |
| ---------------------------------------- | ------ | ----------------------------------------- |
| Input not captured when game not focused | High   | Ensure game window has focus during tests |
| UI z-ordering issues                     | Medium | Use proper render groups (Group31 for UI) |
| Settings blocking ESC propagation        | Medium | Handle ESC in Update before UI processing |
| Slider drag not working via automation   | Medium | Use SetValue RPC instead of mouse drag    |

---

## Success Criteria

1. ✅ Game shows visible green ground plane
2. ✅ Blue player cube is visible at center
3. ✅ WASD/Arrow keys move the player
4. ✅ Position display updates during movement
5. ✅ ESC opens settings overlay
6. ✅ ESC again closes settings
7. ✅ All settings controls are interactable
8. ✅ Existing tests continue to pass
9. ✅ New tests for gameplay and settings pass
