using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brinell.Wpf.VisualValidation;

/// <summary>
/// Manages validation reports for visual testing sessions.
/// Generates reports suitable for AI-powered visual validation.
/// </summary>
public class ValidationReport
{
    private readonly string _outputDirectory;
    private readonly string _sessionId;
    private readonly List<ViewCapture> _captures = new();
    private readonly DateTime _startTime;

    /// <summary>
    /// Report title.
    /// </summary>
    public string Title { get; set; } = "Visual Validation Report";
    
    /// <summary>
    /// Application version being tested.
    /// </summary>
    public string? AppVersion { get; set; }
    
    /// <summary>
    /// Description of the test run.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Path to the design specifications.
    /// </summary>
    public string? DesignSpecPath { get; set; }

    /// <summary>
    /// Creates a new ValidationReport instance.
    /// </summary>
    public ValidationReport(string outputDirectory, string? sessionId = null)
    {
        _outputDirectory = outputDirectory;
        _sessionId = sessionId ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _startTime = DateTime.Now;
        
        Directory.CreateDirectory(GetReportDirectory());
    }

    /// <summary>
    /// Gets the directory for this report.
    /// </summary>
    public string GetReportDirectory()
    {
        return Path.Combine(_outputDirectory, _sessionId);
    }

    /// <summary>
    /// Adds a captured view to the report.
    /// </summary>
    public void AddCapture(ViewCapture capture)
    {
        _captures.Add(capture);
    }

    /// <summary>
    /// Adds multiple captures to the report.
    /// </summary>
    public void AddCaptures(IEnumerable<ViewCapture> captures)
    {
        _captures.AddRange(captures);
    }

    /// <summary>
    /// Generates the validation report as a Markdown file.
    /// </summary>
    public string GenerateMarkdownReport()
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine($"# {Title}");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Session ID:** {_sessionId}");
        if (!string.IsNullOrEmpty(AppVersion))
            sb.AppendLine($"**App Version:** {AppVersion}");
        if (!string.IsNullOrEmpty(Description))
            sb.AppendLine($"**Description:** {Description}");
        sb.AppendLine();
        
        // Instructions for AI validation
        sb.AppendLine("## Instructions for AI Validation");
        sb.AppendLine();
        sb.AppendLine("Review each screenshot below and verify:");
        sb.AppendLine("1. Layout matches the expected design");
        sb.AppendLine("2. Colors and styling are correct");
        sb.AppendLine("3. Text is readable and properly aligned");
        sb.AppendLine("4. Controls are properly sized and positioned");
        sb.AppendLine("5. No visual glitches or rendering issues");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(DesignSpecPath))
        {
            sb.AppendLine($"**Reference Design Spec:** [{DesignSpecPath}]({DesignSpecPath})");
            sb.AppendLine();
        }

        // Summary
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Value |");
        sb.AppendLine($"|--------|-------|");
        sb.AppendLine($"| Total Views | {_captures.Count} |");
        sb.AppendLine($"| Successful Captures | {_captures.Count(c => c.Screenshot.Success)} |");
        sb.AppendLine($"| Failed Captures | {_captures.Count(c => !c.Screenshot.Success)} |");
        sb.AppendLine();

        // Table of views
        sb.AppendLine("## Captured Views");
        sb.AppendLine();
        sb.AppendLine("| View | Status | Size | File |");
        sb.AppendLine("|------|--------|------|------|");
        
        foreach (var capture in _captures)
        {
            var status = capture.Screenshot.Success ? "✅" : "❌";
            var size = capture.Screenshot.Success 
                ? $"{capture.Screenshot.Width}x{capture.Screenshot.Height}" 
                : "N/A";
            var file = capture.Screenshot.Success 
                ? Path.GetFileName(capture.Screenshot.FilePath) 
                : capture.Screenshot.ErrorMessage ?? "Unknown error";
            
            sb.AppendLine($"| {capture.ViewName} | {status} | {size} | {file} |");
        }
        sb.AppendLine();

        // Detailed sections for each view
        sb.AppendLine("## Detailed View Screenshots");
        sb.AppendLine();
        
        foreach (var capture in _captures.Where(c => c.Screenshot.Success))
        {
            sb.AppendLine($"### {capture.ViewName}");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(capture.Description))
            {
                sb.AppendLine($"*{capture.Description}*");
                sb.AppendLine();
            }
            
            // Relative path to image
            var relativePath = Path.GetFileName(capture.Screenshot.FilePath);
            sb.AppendLine($"![{capture.ViewName}]({relativePath})");
            sb.AppendLine();
            
            // Metadata
            if (capture.Metadata.Count > 0)
            {
                sb.AppendLine("**Validation Points:**");
                foreach (var (key, value) in capture.Metadata)
                {
                    sb.AppendLine($"- {key}: {value}");
                }
                sb.AppendLine();
            }
            
            // Validation checklist placeholder
            sb.AppendLine("**Validation Checklist:**");
            sb.AppendLine("- [ ] Layout correct");
            sb.AppendLine("- [ ] Colors match design");
            sb.AppendLine("- [ ] Text readable");
            sb.AppendLine("- [ ] Controls properly positioned");
            sb.AppendLine("- [ ] No visual issues");
            sb.AppendLine();
        }

        // Failures section
        var failures = _captures.Where(c => !c.Screenshot.Success).ToList();
        if (failures.Count > 0)
        {
            sb.AppendLine("## Capture Failures");
            sb.AppendLine();
            foreach (var failure in failures)
            {
                sb.AppendLine($"### ❌ {failure.ViewName}");
                sb.AppendLine();
                sb.AppendLine($"**Error:** {failure.Screenshot.ErrorMessage}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Saves the report as a Markdown file.
    /// </summary>
    public string SaveMarkdownReport(string fileName = "ValidationReport.md")
    {
        var content = GenerateMarkdownReport();
        var filePath = Path.Combine(GetReportDirectory(), fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Generates and saves a JSON manifest of captures.
    /// </summary>
    public string SaveJsonManifest(string fileName = "manifest.json")
    {
        var manifest = new ValidationManifest
        {
            SessionId = _sessionId,
            Title = Title,
            AppVersion = AppVersion,
            Description = Description,
            DesignSpecPath = DesignSpecPath,
            GeneratedAt = DateTime.Now,
            StartTime = _startTime,
            Views = _captures.Select(c => new ViewManifestEntry
            {
                Name = c.ViewName,
                Description = c.Description,
                FileName = c.Screenshot.Success ? Path.GetFileName(c.Screenshot.FilePath!) : null,
                Success = c.Screenshot.Success,
                ErrorMessage = c.Screenshot.ErrorMessage,
                Width = c.Screenshot.Width,
                Height = c.Screenshot.Height,
                Metadata = c.Metadata
            }).ToList()
        };

        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        var json = JsonSerializer.Serialize(manifest, options);
        var filePath = Path.Combine(GetReportDirectory(), fileName);
        File.WriteAllText(filePath, json);
        return filePath;
    }

    /// <summary>
    /// Generates an AI prompt file for visual validation.
    /// </summary>
    public string SaveAIPrompt(string promptTemplate, string fileName = "ai_prompt.md")
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# AI Visual Validation Request");
        sb.AppendLine();
        sb.AppendLine("## Prompt");
        sb.AppendLine();
        sb.AppendLine(promptTemplate);
        sb.AppendLine();
        
        sb.AppendLine("## Screenshots to Validate");
        sb.AppendLine();
        
        foreach (var capture in _captures.Where(c => c.Screenshot.Success))
        {
            var relativePath = Path.GetFileName(capture.Screenshot.FilePath);
            sb.AppendLine($"### {capture.ViewName}");
            sb.AppendLine();
            sb.AppendLine($"![{capture.ViewName}]({relativePath})");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(capture.Description))
            {
                sb.AppendLine($"Description: {capture.Description}");
                sb.AppendLine();
            }
        }

        var filePath = Path.Combine(GetReportDirectory(), fileName);
        File.WriteAllText(filePath, sb.ToString());
        return filePath;
    }
}

/// <summary>
/// JSON manifest for validation session.
/// </summary>
public class ValidationManifest
{
    public required string SessionId { get; init; }
    public required string Title { get; init; }
    public string? AppVersion { get; init; }
    public string? Description { get; init; }
    public string? DesignSpecPath { get; init; }
    public DateTime GeneratedAt { get; init; }
    public DateTime StartTime { get; init; }
    public List<ViewManifestEntry> Views { get; init; } = new();
}

/// <summary>
/// Manifest entry for a single view.
/// </summary>
public class ViewManifestEntry
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? FileName { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
