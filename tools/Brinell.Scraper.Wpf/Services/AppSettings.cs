using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _settingsPath;
    private readonly ILogger<AppSettings> _logger;

    public AppSettings(ILogger<AppSettings> logger)
    {
        _logger = logger;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Brinell.Scraper");
        Directory.CreateDirectory(dir);

        _settingsPath = Path.Combine(dir, "settings.json");
        CorpusRoot = dir;
        SkillsRoot = Path.Combine(dir, "skills");

        Load();
    }

    public string AnalyzerModel { get; set; } = "claude-haiku-4.5";
    public string GeneratorModel { get; set; } = "claude-haiku-4.5";
    public bool LogLlmPrompts { get; set; }
    public bool LogLlmResponses { get; set; }
    public string CorpusRoot { get; set; }
    public string SkillsRoot { get; set; }

    public void Save()
    {
        try
        {
            var dto = new SettingsDto
            {
                AnalyzerModel = AnalyzerModel,
                GeneratorModel = GeneratorModel,
                LogLlmPrompts = LogLlmPrompts,
                LogLlmResponses = LogLlmResponses,
            };
            var json = JsonSerializer.Serialize(dto, s_jsonOptions);
            File.WriteAllText(_settingsPath, json);
            _logger.LogInformation("AppSettings saved — Path: {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AppSettings save failed — Path: {Path}", _settingsPath);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return;

            var json = File.ReadAllText(_settingsPath);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json, s_jsonOptions);
            if (dto is null)
                return;

            if (!string.IsNullOrWhiteSpace(dto.AnalyzerModel)) AnalyzerModel = dto.AnalyzerModel;
            if (!string.IsNullOrWhiteSpace(dto.GeneratorModel)) GeneratorModel = dto.GeneratorModel;
            LogLlmPrompts = dto.LogLlmPrompts;
            LogLlmResponses = dto.LogLlmResponses;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AppSettings load failed — Path: {Path}", _settingsPath);
        }
    }

    private sealed class SettingsDto
    {
        public string AnalyzerModel { get; set; } = "";
        public string GeneratorModel { get; set; } = "";
        public bool LogLlmPrompts { get; set; }
        public bool LogLlmResponses { get; set; }
    }
}
