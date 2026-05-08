using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class SkillService
{
    private readonly IControlRegistry _registry;
    private readonly AppSettings _settings;
    private readonly ILogger<SkillService> _logger;

    public SkillService(
        IControlRegistry registry,
        AppSettings settings,
        ILogger<SkillService> logger)
    {
        _registry = registry;
        _settings = settings;
        _logger = logger;
    }

    public void EnsureBrinellConventionsSkill()
    {
        var dir = Path.Combine(_settings.SkillsRoot, "brinell-conventions");
        Directory.CreateDirectory(dir);

        var skillPath = Path.Combine(dir, "SKILL.md");
        if (!File.Exists(skillPath))
        {
            File.WriteAllText(skillPath, BrinellConventionsContent);
            _logger.LogInformation("Created brinell-conventions skill at {Path}", skillPath);
        }
    }

    public async Task GenerateSiteControlsSkillAsync(
        long siteId, string siteSlug, CancellationToken ct = default)
    {
        // TODO: filter by siteId once IControlRegistry exposes per-site queries.
        _ = siteId;
        var controls = _registry.GetAllControls();

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
            sb.AppendLine();
            sb.AppendLine("Usage:");
            sb.AppendLine("```csharp");
            sb.AppendLine($"public {c.Name}<MyPage> {SuggestPropertyName(c.Name)} =>");
            sb.AppendLine($"    Control<{c.Name}<MyPage>>(Locator.ByCss(\"{c.DomSignature}\"));");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(skillPath, sb.ToString(), ct);

        _logger.LogInformation(
            "Generated skill {Path} with {Count} controls",
            skillPath, controls.Count);
    }

    private static string SuggestPropertyName(string controlName) =>
        controlName.EndsWith("Container", StringComparison.Ordinal)
            ? controlName[..^"Container".Length]
            : controlName;

    private const string BrinellConventionsContent = """
        # Brinell Framework Conventions

        ## Base Classes

        - `HtmlPageObjectBase<TSelf>` — base for all page objects
        - `ContainerBase<TParent, TScope>` — base for container/composite controls

        ## Built-in Control Types

        All controls are generic with a `<TScope>` type parameter:

        | Control | HTML Element |
        |---------|-------------|
        | `TextInputControl<TScope>` | `<input type="text/email/number/...">` |
        | `ButtonControl<TScope>` | `<button>`, `<input type="submit">` |
        | `SelectControl<TScope>` | `<select>` |
        | `LabelControl<TScope>` | `<label>`, text-only elements |
        | `CheckBoxControl<TScope>` | `<input type="checkbox">` |
        | `RadioButtonControl<TScope>` | `<input type="radio">` |
        | `LinkControl<TScope>` | `<a href="...">` |
        | `FileInputControl<TScope>` | `<input type="file">` |
        | `TextAreaControl<TScope>` | `<textarea>` |
        | `ImageControl<TScope>` | `<img>` |
        | `ElementControl<TScope>` | Generic fallback for any element |

        ## Locator Strategies (preference order)

        1. `Locator.ByText("value")` / `Locator.ByLinkText("value")` / `Locator.ByPartialLinkText("value")` — primary, most resilient
        2. `Locator.ByDataTestId("value")` — explicit test hooks
        3. `Locator.ByAriaLabel("value")` — accessibility attributes
        4. `Locator.ById("value")` — only if stable/not dynamically generated
        5. `Locator.ByCss("selector")` — last resort, emit warning

        ## Code Style Rules

        - `sealed` classes
        - Expression-bodied properties for controls
        - PascalCase property names derived from element labels/ids
        - One class per file
        - File-scoped namespaces

        ## Example PageObject

        ```csharp
        using Brinell.Core.Locators;
        using Brinell.Html.Controls;

        namespace ExactOnline.Pages;

        public sealed class LoginPage : HtmlPageObjectBase<LoginPage>
        {
            public LoginPage(IHtmlTestContext context) : base(context) { }

            public TextInputControl<LoginPage> UserName =>
                Control<TextInputControl<LoginPage>>(Locator.ByText("User name"));

            public TextInputControl<LoginPage> Password =>
                Control<TextInputControl<LoginPage>>(Locator.ByText("Password"));

            public ButtonControl<LoginPage> SignIn =>
                Control<ButtonControl<LoginPage>>(Locator.ByText("Sign in"));
        }
        ```

        ## Example ContainerBase

        ```csharp
        using Brinell.Core.Locators;
        using Brinell.Html.Controls;

        namespace ExactOnline.Controls;

        public sealed class SearchBarContainer<TParent>
            : ContainerBase<TParent, SearchBarContainer<TParent>>
        {
            public TextInputControl<SearchBarContainer<TParent>> SearchInput =>
                Control<TextInputControl<SearchBarContainer<TParent>>>(
                    Locator.ByAriaLabel("Search"));

            public ButtonControl<SearchBarContainer<TParent>> SearchButton =>
                Control<ButtonControl<SearchBarContainer<TParent>>>(
                    Locator.ByText("Search"));
        }
        ```
        """;
}
