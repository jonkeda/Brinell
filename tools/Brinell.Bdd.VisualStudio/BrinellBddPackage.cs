using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Brinell.Bdd.VisualStudio;

/// <summary>
/// The extension's package: the Scenario Preview tool window and its commands. It loads in the
/// background when a feature or step file is selected or opened, and starts the language server
/// the first time one becomes the active document.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageIds.Package)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(ScenarioPreviewWindow), Style = VsDockStyle.Tabbed, Window = "{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}", Orientation = ToolWindowOrientation.Right)]
[ProvideToolWindow(typeof(BddExplorerWindow), Style = VsDockStyle.Tabbed, Window = "{3AE79031-E1BC-11D0-8F78-00A0C9110057}")]
[ProvideAutoLoad(PackageIds.FeatureFileContext, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideUIContextRule(
    PackageIds.FeatureFileContext,
    name: "Brinell feature or step file",
    expression: "Feature | Steps",
    termNames: ["Feature", "Steps"],
    termValues: [@"HierSingleSelectionName:\.feature\.md$", @"HierSingleSelectionName:\.steps\.md$"])]
public sealed class BrinellBddPackage : AsyncPackage
{
    private PreviewController? _controller;

    /// <summary>The package once loaded, for CodeLens's callback, which runs outside it.</summary>
    internal static BrinellBddPackage? Instance { get; private set; }

    /// <summary>The preview's controller, which holds the language server.</summary>
    internal PreviewController? Controller => _controller;

    /// <summary>
    /// The package, loaded if it is not yet: an editor's preview margin can be created before the
    /// package's background load has run.
    /// </summary>
    internal static async Task<BrinellBddPackage?> LoadAsync()
    {
        if (Instance is { } loaded)
        {
            return loaded;
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsShell)) is IVsShell7 shell)
        {
            var guid = new Guid(PackageIds.Package);
            await shell.LoadPackageAsync(ref guid);
        }

        return Instance;
    }

    /// <inheritdoc />
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _controller = new PreviewController(this);
        await _controller.StartAsync(cancellationToken);
        Instance = this;

        if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commands)
        {
            commands.AddCommand(new MenuCommand((_, _) => ShowPreview(), new CommandID(PackageIds.CommandSet, PackageIds.ShowScenarioPreview)));
            var open = new OleMenuCommand((_, _) => _controller.TogglePreviewMargin(), new CommandID(PackageIds.CommandSet, PackageIds.OpenScenarioPreview));
            open.BeforeQueryStatus += (_, _) => open.Visible = _controller.ActiveIsFeatureOrSteps;
            commands.AddCommand(open);

            commands.AddCommand(new MenuCommand((_, _) => ShowExplorer(), new CommandID(PackageIds.CommandSet, PackageIds.ShowBddExplorer)));

            var toggle = new OleMenuCommand((_, _) => ToggleStepBreakpoint(), new CommandID(PackageIds.CommandSet, PackageIds.ToggleStepBreakpoint));
            toggle.BeforeQueryStatus += (_, _) => toggle.Visible = toggle.Enabled = _controller.ActiveIsFeatureOrSteps;
            commands.AddCommand(toggle);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (disposing)
        {
            _controller?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ToggleStepBreakpoint()
        => JoinableTaskFactory.RunAsync(() => _controller!.ToggleBreakpointAtCaretAsync()).FileAndForget("Brinell/ToggleStepBreakpoint");

    private void ShowExplorer()
    {
        JoinableTaskFactory.RunAsync(async () =>
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            var window = await ShowToolWindowAsync(typeof(BddExplorerWindow), 0, create: true, DisposalToken);
            if (window is BddExplorerWindow explorer && _controller is not null)
            {
                await _controller.AttachExplorerAsync(explorer.Control);
            }
        }).FileAndForget("Brinell/ShowBddExplorer");
    }

    private void ShowPreview()
    {
        JoinableTaskFactory.RunAsync(async () =>
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            var window = await ShowToolWindowAsync(typeof(ScenarioPreviewWindow), 0, create: true, DisposalToken);
            if (window is ScenarioPreviewWindow preview && _controller is not null)
            {
                await _controller.AttachAsync(preview.Control);
            }
        }).FileAndForget("Brinell/ShowScenarioPreview");
    }
}
