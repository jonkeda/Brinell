namespace Brinell.Scraper.Models;

public sealed record PipelineProgress(string Stage, int Current, int Total);
