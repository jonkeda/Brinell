using Stride.Core;
using Stride.Engine;
using Stride.Games;
using Stride.UI;

namespace Brinell.Stride.Automation;

/// <summary>
/// Stride game system that runs the automation server.
/// Add this to your game's systems to enable automation.
/// </summary>
public class AutomationGameSystem : GameSystemBase
{
    private AutomationServer? _server;
    private readonly AutomationServerOptions _options;
    private readonly Func<UIElement?>? _uiRootProvider;
    private readonly IAutomationHandler? _customHandler;
    private readonly IGame? _game;
    private bool _initialized;

    /// <summary>
    /// Create with default UI handler.
    /// </summary>
    public AutomationGameSystem(
        IServiceRegistry registry,
        Func<UIElement?> uiRootProvider,
        AutomationServerOptions? options = null,
        IGame? game = null)
        : base(registry)
    {
        _uiRootProvider = uiRootProvider ?? throw new ArgumentNullException(nameof(uiRootProvider));
        _options = options ?? new AutomationServerOptions();
        _game = game;
    }

    /// <summary>
    /// Create with custom handler.
    /// </summary>
    public AutomationGameSystem(
        IServiceRegistry registry,
        IAutomationHandler handler,
        AutomationServerOptions? options = null)
        : base(registry)
    {
        _customHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        _options = options ?? new AutomationServerOptions();
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        // Prevent double initialization - Stride can call this multiple times
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        IAutomationHandler handler;
        if (_customHandler != null)
        {
            handler = _customHandler;
        }
        else if (_uiRootProvider != null)
        {
            handler = new StrideUIHandler(_uiRootProvider, game: _game);
        }
        else
        {
            throw new InvalidOperationException("No handler or UI root provider specified");
        }

        _server = new AutomationServer(handler, _options);
        _server.Start();
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        _server?.StopAsync().GetAwaiter().GetResult();
        _server?.Dispose();
        base.Destroy();
    }
}
