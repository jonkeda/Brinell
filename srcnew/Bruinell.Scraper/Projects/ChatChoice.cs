namespace Bruinell.Scraper.Projects;

/// <summary>The chat agent (or mode) and the model the Scraper's prompts run with; null for the default.</summary>
/// <param name="Agent">A custom agent's name (<c>bruinell-scraper</c>) or a mode (<c>agent</c>, <c>ask</c>).</param>
/// <param name="Model">A model's id; empty or null for the IDE's own.</param>
public sealed record ChatChoice(string? Agent, string? Model);
