using System.IO;
using System.Windows;
using Brinell.Scraper.Data;
using Brinell.Scraper.Logging;
using Brinell.Scraper.Services;
using Brinell.Scraper.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Brinell.Scraper;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            var services = new ServiceCollection();

        // Logging (Step 3.1 + 3.2)
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "scraper-.json");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                formatter: new Serilog.Formatting.Json.JsonFormatter())
            .CreateLogger();

        var inAppLogService = new InAppLogService();
        services.AddSingleton(inAppLogService);
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddDebug();
            builder.AddSerilog(dispose: true);
            builder.AddProvider(new InAppLogProvider(inAppLogService));
        });

        // Data
        services.AddSingleton<CorpusDatabase>();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Brinell.Scraper", "scraper.db");
        var connectionString = $"Data Source={dbPath}";

        // Services (Phase 4)
        services.AddSingleton<DomCaptureService>();
        services.AddSingleton<ElementHighlightService>();
        services.AddSingleton<PageTransitionDetector>();
        services.AddSingleton<ControlGroupDetector>();
        services.AddSingleton<DomDiffService>();
        services.AddSingleton<SnapshotExportService>();

        // Services (Phase 5 — LLM Code Generation)
        services.AddSingleton<ICopilotService, CopilotService>();
        services.AddSingleton<IControlRegistry>(sp =>
            new ControlRegistry(connectionString, sp.GetRequiredService<ILogger<ControlRegistry>>()));
        services.AddSingleton<CorpusService>(sp =>
            new CorpusService(connectionString, sp.GetRequiredService<ILogger<CorpusService>>()));
        services.AddSingleton<CorpusTools>();
        services.AddSingleton<SkillService>(sp =>
        {
            var skillsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Brinell.Scraper", "skills");
            return new SkillService(skillsDir, sp.GetRequiredService<ILogger<SkillService>>());
        });
        services.AddSingleton<RetryService>();
        services.AddSingleton<PromptBuilder>();
        services.AddSingleton<AnalysisService>();
        services.AddSingleton<ControlGenerationService>();
        services.AddSingleton<PageGenerationService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SiteSelectionViewModel>();
        services.AddTransient<BrowserViewModel>();
        services.AddSingleton<SidebarViewModel>();
        services.AddTransient<InspectorViewModel>();
        services.AddTransient<CodePreviewViewModel>();
        services.AddSingleton<RecordingViewModel>();
        services.AddTransient<AnalysisViewModel>();
        services.AddTransient<ControlsManagerViewModel>();
        services.AddTransient<GenerationViewModel>();
        services.AddTransient<CorpusBrowserViewModel>();
        services.AddSingleton<LogViewerViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
