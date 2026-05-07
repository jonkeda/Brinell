# Step 12.8 — Settings Tab

## Objective

Dedicated tab for site-specific and app-wide settings. Replaces ad-hoc settings windows. Reachable both from the Workspace tab strip and from the Start Page settings link.

## Dependencies

- Step 12.2 (Workspace shell), Step 12.1 (Start Page link)
- Existing `AppSettings`, `SiteService`

## Implementation

### Files

- `Views/Tabs/SettingsTabView.xaml`
- `ViewModels/SettingsTabViewModel.cs`

### `SettingsTabViewModel`

```csharp
public class SettingsTabViewModel : ViewModelBase
{
    // Site
    public string SiteName { get; set; } = "";
    public string StartUrl { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public string TargetNamespace { get; set; } = "";

    // App
    public string AnalyzerModel { get; set; } = "gpt-4o-mini";
    public string GeneratorModel { get; set; } = "gpt-4o";
    public bool LogLlmPrompts { get; set; }
    public bool LogLlmResponses { get; set; }
    public string CorpusRoot { get; set; } = "";
    public string SkillsRoot { get; set; } = "";

    public IAsyncCommand SaveCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand BrowseOutputPathCommand { get; }
    public ICommand SignInToGitHubCommand { get; }
    public bool IsCopilotAuthenticated { get; }

    public void Load(long siteId);
}
```

### Layout

```
ScrollViewer
└─ StackPanel (Vertical, Margin)
    ├─ GroupBox "Site"
    │   - Name TextBox
    │   - Start URL TextBox
    │   - Output Path TextBox + [Browse]
    │   - Target Namespace TextBox
    ├─ GroupBox "Models"
    │   - Analyzer model ComboBox
    │   - Generator model ComboBox
    ├─ GroupBox "Logging"
    │   - Log LLM prompts (CheckBox)
    │   - Log LLM responses (CheckBox)
    ├─ GroupBox "Paths"
    │   - Corpus root (read-only)
    │   - Skills root (read-only)
    ├─ GroupBox "GitHub Copilot"
    │   - Status label (Authenticated / Not signed in)
    │   - [Sign in to GitHub] button
    └─ Buttons row: [Save] [Reset]
```

### Behavior

- `Load(siteId)` populates site + app properties.
- `SaveCommand` persists site fields via `SiteService.UpdateAsync`, app fields via `AppSettings`.
- `Reset` reverts to last-saved values.
- `SignInToGitHubCommand` triggers the Copilot SDK auth flow and refreshes `IsCopilotAuthenticated`.
- When opened from Start Page (no active site), site GroupBox is hidden.

## Checklist

- [ ] ViewModel exposes site + app fields and commands
- [ ] View groups settings into Site / Models / Logging / Paths / GitHub
- [ ] Save persists and reloads
- [ ] Reset restores prior values
- [ ] GitHub auth button reflects current state
- [ ] Tab works both inside Workspace and as standalone (from Start Page)
