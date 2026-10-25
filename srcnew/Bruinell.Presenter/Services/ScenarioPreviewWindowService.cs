using System.Text.Json;
using Bruinell.Bdd.Preview;

namespace Bruinell.Presenter.Services;

/// <summary>
/// The Scenario Preview in a window of its own: the IDEs' page (<see cref="BddPreview.HostPage"/>)
/// in a WebView2, sent the same messages the IDEs send (<c>update</c>, <c>scrollTo</c>,
/// <c>running</c>, <c>paused</c>, <c>theme</c>).
/// </summary>
public sealed class ScenarioPreviewWindowService : IScenarioPreviewService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private Window? _window;
    private bool _ready;
    private ScenarioPreviewState? _last;
#if WINDOWS
    private Microsoft.UI.Xaml.Controls.WebView2? _webView2;
#endif

    /// <inheritdoc />
    public bool IsOpen => _window is not null;

    /// <inheritdoc />
    public event Action? Ready;

    /// <inheritdoc />
    public event Action<string, JsonElement>? Message;

    /// <inheritdoc />
    public void Open()
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        if (_window is not null)
        {
            application.ActivateWindow(_window);
            return;
        }

        _ready = false;
        var webView = new WebView
        {
            AutomationId = "ScenarioPreviewWebView",
            Source = new HtmlWebViewSource { Html = BddPreview.HostPage },
        };
        webView.HandlerChanged += (_, _) => Attach(webView);
        _window = new Window(new ContentPage { Title = "Scenario Preview", Content = webView })
        {
            Title = "Scenario Preview",
            Width = 720,
            Height = 900,
        };
        _window.Destroying += (_, _) =>
        {
            _window = null;
            _ready = false;
#if WINDOWS
            _webView2 = null;
#endif
        };
        application.OpenWindow(_window);
    }

    /// <inheritdoc />
    public void Show(ScenarioPreviewState state)
    {
        _last = state;
        if (!_ready)
        {
            return;
        }

        string text;
        try
        {
            text = File.ReadAllText(state.FilePath);
        }
        catch (IOException)
        {
            return;
        }

        var runs = state.Run is { } run
            ? new Dictionary<string, BddScenarioRun> { [run.Scenario] = run }
            : new Dictionary<string, BddScenarioRun>();
        var result = BddPreview.Render(text, state.FilePath, new BddPreviewOptions
        {
            Catalogs = state.Catalogs,
            Runs = runs,
            ImageUrl = _ => null,
            ParseOnlyReason = state.Catalogs is null ? "The suite is not built, so its steps are not bound." : null,
        });

        Post(new { type = "theme", kind = state.Dark ? "dark" : "light" });
        Post(new
        {
            type = "update",
            html = result.Html,
            suites = new[] { state.Suite },
            suite = state.Suite,
            bindState = state.Catalogs is null ? "parse-only" : "bound",
            reason = state.Catalogs is null ? "The suite is not built." : null,
        });
        Post(new { type = "running", line = state.CurrentLine is null ? (int?)null : state.ScenarioLine });
        if (state.Paused is { } paused)
        {
            Post(new
            {
                type = "paused",
                text = paused.Text,
                line = paused.Line,
                here = true,
                isCall = paused.IsCall,
                depth = paused.InCall ? 1 : 0,
                calls = Array.Empty<string>(),
                app = paused.IsCall ? null : paused.Details?.App,
                page = paused.Details?.Page,
                arguments = paused.IsCall ? null : paused.Details?.Arguments,
            });
        }
        else
        {
            Post(new { type = "paused" });
            Post(new { type = "scrollTo", line = state.CurrentLine ?? state.ScenarioLine });
        }
    }

    private void Attach(WebView webView)
    {
#if WINDOWS
        if (webView.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.WebView2 webView2)
        {
            return;
        }

        _webView2 = webView2;
        if (webView2.CoreWebView2 is not null)
        {
            webView2.CoreWebView2.WebMessageReceived += OnWebMessage;
        }
        else
        {
            webView2.CoreWebView2Initialized += (sender, _) => sender.CoreWebView2.WebMessageReceived += OnWebMessage;
        }
#endif
    }

#if WINDOWS
    private void OnWebMessage(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
    {
        using var document = JsonDocument.Parse(args.WebMessageAsJson);
        var message = document.RootElement.Clone();
        var type = message.TryGetProperty("type", out var value) ? value.GetString() ?? string.Empty : string.Empty;
        if (type == "ready")
        {
            _ready = true;
            if (_last is { } last)
            {
                Show(last);
            }

            Ready?.Invoke();
            return;
        }

        Message?.Invoke(type, message);
    }
#endif

    private void Post(object message)
    {
#if WINDOWS
        _webView2?.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(message, Json));
#endif
    }
}
