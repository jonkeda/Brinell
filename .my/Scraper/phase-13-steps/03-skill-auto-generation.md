# Step 13.3 — Skill Auto-Generation for Site Controls

## Objective

After ControlObjects are generated, write a `{site}-controls/SKILL.md` file describing each control. The Copilot SDK loads this skill into the **generator** agent so PageObject generation knows which custom controls to use.

## Dependencies

- Step 13.2 (generated controls in `IControlRegistry`)
- `SkillsRoot` config (default: `./corpus/skills/`)

## Implementation

### Files

- `Services/SkillService.cs`

### Service contract

```csharp
public class SkillService
{
    private readonly IControlRegistry _registry;
    private readonly AppSettings _settings;
    private readonly ILogger<SkillService> _logger;

    public async Task GenerateSiteControlsSkillAsync(
        long siteId, string siteSlug, CancellationToken ct = default)
    {
        var controls = await _registry.GetControlsAsync(siteId, ct);
        var skillDir = Path.Combine(_settings.SkillsRoot, $"{siteSlug}-controls");
        Directory.CreateDirectory(skillDir);
        var skillPath = Path.Combine(skillDir, "SKILL.md");

        var sb = new StringBuilder();
        sb.AppendLine($"# {siteSlug} — Custom Control Objects");
        sb.AppendLine();
        sb.AppendLine("These ControlObjects are available to use as typed properties in PageObject classes for this site.");
        sb.AppendLine();
        foreach (var c in controls)
        {
            sb.AppendLine($"## {c.Name}");
            sb.AppendLine($"- DOM signature: `{c.DomSignature}`");
            sb.AppendLine($"- Found on: {string.Join(", ", c.PageUrls)}");
            sb.AppendLine($"- Properties: " + string.Join(", ",
                c.SuggestedProperties.Select(p => $"{p.Name} ({p.ControlType})")));
            sb.AppendLine();
            sb.AppendLine("Usage:");
            sb.AppendLine("```csharp");
            sb.AppendLine($"public {c.Name}<MyPage> {SuggestPropertyName(c.Name)} =>");
            sb.AppendLine($"    Control<{c.Name}<MyPage>>(Locator.ByCss(\"{c.DomSignature}\"));");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(skillPath, sb.ToString(), ct);
        _logger.LogInformation("Generated skill {Path} with {Count} controls", skillPath, controls.Count);
    }

    private static string SuggestPropertyName(string controlName) =>
        controlName.EndsWith("Container") ? controlName[..^"Container".Length] : controlName;
}
```

### File layout

```
{SkillsRoot}/
├─ brinell-conventions/
│   └─ SKILL.md             # static, ships with app
└─ {site-slug}-controls/
    └─ SKILL.md             # auto-generated, regenerated after every control gen run
```

### When to regenerate

| Trigger | Action |
|---|---|
| New controls generated | Regenerate full file (overwrite) |
| Control deleted | Regenerate full file |
| Control renamed | Regenerate full file |
| Site renamed | Move directory, regenerate |

### Copilot SDK consumption

Skill directory pattern is registered in `CopilotService.InitializeAsync`:

```csharp
SkillDirectories = new[] { _settings.SkillsRoot },
CustomAgents = new[]
{
    new AgentConfig { Name = "generator",
        Skills = new[] { "brinell-conventions", $"{siteSlug}-controls" } }
}
```

### DI registration

```csharp
services.AddSingleton<SkillService>();
```

## Checklist

- [ ] `SkillService.GenerateSiteControlsSkillAsync` writes `{slug}-controls/SKILL.md`
- [ ] Skill content lists each control with signature, properties, usage example
- [ ] File regenerated (overwritten) on every control gen, delete, rename
- [ ] `SkillsRoot` path comes from `AppSettings`
- [ ] Generator agent loads `{site-slug}-controls` skill in Copilot session
- [ ] Service registered in DI
- [ ] Logging includes path and control count
