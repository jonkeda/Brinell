using System.IO;
using System.Text;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class SkillService
{
    private readonly string _skillsDirectory;
    private readonly ILogger<SkillService> _logger;

    public SkillService(string skillsDirectory, ILogger<SkillService> logger)
    {
        _skillsDirectory = skillsDirectory;
        _logger = logger;
    }

    public void EnsureBrinellConventionsSkill()
    {
        var dir = Path.Combine(_skillsDirectory, "brinell-conventions");
        Directory.CreateDirectory(dir);

        var skillPath = Path.Combine(dir, "SKILL.md");
        if (!File.Exists(skillPath))
        {
            File.WriteAllText(skillPath, BrinellConventionsContent);
            _logger.LogInformation("Created brinell-conventions skill at {Path}", skillPath);
        }
    }

    public void GenerateSiteControlsSkill(string siteName, IReadOnlyList<GeneratedControl> controls)
    {
        var dir = Path.Combine(_skillsDirectory, $"{siteName}-controls");
        Directory.CreateDirectory(dir);

        var content = BuildSiteControlsSkillContent(siteName, controls);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), content);

        _logger.LogInformation(
            "Generated {SiteName}-controls skill with {ControlCount} controls",
            siteName, controls.Count);
    }

    private static string BuildSiteControlsSkillContent(
        string siteName, IReadOnlyList<GeneratedControl> controls)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {siteName} — Custom Controls");
        sb.AppendLine();
        sb.AppendLine("Use these site-specific controls when their DOM patterns are detected.");
        sb.AppendLine();

        foreach (var ctrl in controls)
        {
            sb.AppendLine($"## {ctrl.Name}");
            sb.AppendLine();
            sb.AppendLine($"**DOM signature:** `{ctrl.DomSignature}`");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(ctrl.Code);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        return sb.ToString();
    }

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
