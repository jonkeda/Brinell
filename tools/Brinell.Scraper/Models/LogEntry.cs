using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Models;

public sealed record LogEntry(DateTime Timestamp, LogLevel Level, string Source, string Message);
