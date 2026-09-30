using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Logging;

public sealed class InAppLogProvider : ILoggerProvider
{
    private readonly InAppLogService _service;

    public InAppLogProvider(InAppLogService service)
    {
        _service = service;
    }

    public ILogger CreateLogger(string categoryName) => new InAppLogger(_service, categoryName);

    public void Dispose() { }

    private sealed class InAppLogger : ILogger
    {
        private readonly InAppLogService _service;
        private readonly string _source;

        public InAppLogger(InAppLogService service, string categoryName)
        {
            _service = service;
            // Extract short source name from full category (e.g. "Brinell.Scraper.ViewModels.BrowserViewModel" → "BrowserViewModel")
            var lastDot = categoryName.LastIndexOf('.');
            _source = lastDot >= 0 ? categoryName[(lastDot + 1)..] : categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            if (exception is not null)
                message += Environment.NewLine + exception;

            _service.Add(new LogEntry(DateTime.Now, logLevel, _source, message));
        }
    }
}
