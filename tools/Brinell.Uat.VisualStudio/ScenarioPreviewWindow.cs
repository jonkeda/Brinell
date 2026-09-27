using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Brinell.Uat.VisualStudio;

/// <summary>The Scenario Preview tool window: a WebView2 showing the language server's preview.</summary>
[Guid(PackageIds.ScenarioPreviewWindow)]
public sealed class ScenarioPreviewWindow : ToolWindowPane
{
    /// <summary>Creates the window.</summary>
    public ScenarioPreviewWindow()
        : base(null)
    {
        Caption = "Scenario Preview";
        Control = new ScenarioPreviewControl();
        Content = Control;
    }

    /// <summary>The window's content.</summary>
    public ScenarioPreviewControl Control { get; }
}

/// <summary>
/// The WebView2 and the messages to and from its page (the host page of <c>Brinell.Uat.Preview</c>).
/// Messages posted before the page is ready wait for it.
/// </summary>
public sealed class ScenarioPreviewControl : UserControl
{
    private static readonly JsonSerializerSettings MessageJson = new() { ContractResolver = new CamelCasePropertyNamesContractResolver(), NullValueHandling = NullValueHandling.Ignore };

    // Set once per process: WebView2 refuses a second call after the loader is loaded.
    private static bool _loaderSet;

    private readonly WebView2 _web = new();
    private readonly List<string> _pending = [];
    private bool _ready;
    private string? _mappedResults;
    private Task? _initializing;

    /// <summary>Creates the control; the WebView2 starts when <see cref="InitializeAsync"/> is called.</summary>
    public ScenarioPreviewControl() => Content = _web;

    /// <summary>A message from the page: <c>reveal</c>, <c>pickSuite</c>, <c>ready</c>.</summary>
    public event EventHandler<JObject>? MessageReceived;

    /// <summary>Whether the page has loaded and taken the first messages.</summary>
    public bool IsReady => _ready;

    /// <summary>
    /// Starts the WebView2 with its user data under <c>%LOCALAPPDATA%\Brinell\WebView2</c>, and loads
    /// the host page. Called once; later calls wait for the first.
    /// </summary>
    /// <param name="hostPage">The page, from the server's <c>brinell/previewHost</c>.</param>
    public Task InitializeAsync(string hostPage) => _initializing ??= StartAsync(hostPage);

    /// <summary>Posts a message to the page, or keeps it until the page is ready.</summary>
    /// <param name="message">An object with a <c>type</c>.</param>
    public void Post(object message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var json = JsonConvert.SerializeObject(message, MessageJson);
        if (_ready && _web.CoreWebView2 is { } core)
        {
            core.PostWebMessageAsJson(json);
        }
        else
        {
            _pending.Add(json);
        }
    }

    /// <summary>
    /// Maps the preview's screenshot host to the results folder, so its images load: a webview does
    /// not load <c>file:</c> URLs from a page it was given as a string.
    /// </summary>
    /// <param name="host">The virtual host the server's URLs use.</param>
    /// <param name="root">The results folder.</param>
    public void MapResults(string host, string root)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_web.CoreWebView2 is not { } core || string.Equals(_mappedResults, root, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(root))
        {
            return;
        }

        core.SetVirtualHostNameToFolderMapping(host, root, CoreWebView2HostResourceAccessKind.Allow);
        _mappedResults = root;
    }

    private async Task StartAsync(string hostPage)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brinell", "WebView2");

        // The loader is carried in the VSIX under runtimes\win-<arch>\native, where .NET Framework does not look by itself.
        var architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : Environment.Is64BitProcess ? "x64" : "x86";
        var loader = Path.Combine(Path.GetDirectoryName(typeof(ScenarioPreviewControl).Assembly.Location)!, "runtimes", "win-" + architecture, "native");
        if (!_loaderSet && Directory.Exists(loader))
        {
            CoreWebView2Environment.SetLoaderDllFolderPath(loader);
            _loaderSet = true;
        }

        var environment = await CoreWebView2Environment.CreateAsync(browserExecutableFolder: null, userDataFolder: userData);
        await _web.EnsureCoreWebView2Async(environment);
        var core = _web.CoreWebView2;
        core.Settings.AreDevToolsEnabled = true;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.WebMessageReceived += OnWebMessage;
        core.NavigateToString(hostPage);
    }

    private void OnWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var message = JObject.Parse(e.WebMessageAsJson);
        if ((string?)message["type"] == "ready")
        {
            _ready = true;
            foreach (var json in _pending)
            {
                _web.CoreWebView2.PostWebMessageAsJson(json);
            }

            _pending.Clear();
        }

        MessageReceived?.Invoke(this, message);
    }
}
