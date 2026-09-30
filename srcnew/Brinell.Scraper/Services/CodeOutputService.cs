using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class CodeOutputService
{
    private const string CsprojTemplate = """
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{ROOT_NAMESPACE}</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Brinell.Core" Version="*" />
    <PackageReference Include="Brinell.Html" Version="*" />
  </ItemGroup>

</Project>
""";

    private static readonly Regex ContainerClassRegex = new(@"class\s+(\w+)", RegexOptions.Compiled);

    private readonly ILogger<CodeOutputService> _logger;

    public CodeOutputService(ILogger<CodeOutputService> logger)
    {
        _logger = logger;
    }

    public async Task WriteProjectAsync(
        string outputPath,
        string targetNamespace,
        IReadOnlyList<GeneratedControl> controls,
        IReadOnlyList<PageGenerationResult> pages,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
        if (string.IsNullOrWhiteSpace(targetNamespace))
            throw new ArgumentException("Target namespace cannot be null or empty.", nameof(targetNamespace));

        var fullPath = Path.GetFullPath(outputPath);
        var root = Path.GetPathRoot(fullPath);
        if (!string.IsNullOrEmpty(root) && string.Equals(
                fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Output path cannot be a drive root.", nameof(outputPath));
        }

        if (fullPath.Contains(@"\corpus\", StringComparison.OrdinalIgnoreCase) ||
            fullPath.Contains("/corpus/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Output path cannot be inside the corpus directory.", nameof(outputPath));
        }

        ct.ThrowIfCancellationRequested();

        Directory.CreateDirectory(fullPath);
        var controlsDir = Path.Combine(fullPath, "Controls");
        var pagesDir = Path.Combine(fullPath, "Pages");
        var containersDir = Path.Combine(pagesDir, "Containers");
        Directory.CreateDirectory(controlsDir);
        Directory.CreateDirectory(pagesDir);
        Directory.CreateDirectory(containersDir);

        var timestamp = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        // csproj — always overwrite.
        var csprojName = $"{targetNamespace}.csproj";
        var csprojPath = Path.Combine(fullPath, csprojName);
        var csprojContent = CsprojTemplate.Replace("{ROOT_NAMESPACE}", targetNamespace);
        await WriteFileAtomicAsync(csprojPath, csprojContent, ct);

        // Controls — alphabetical by Name.
        var orderedControls = controls.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
        var controlFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var control in orderedControls)
        {
            ct.ThrowIfCancellationRequested();
            var fileName = $"{control.Name}.cs";
            var path = Path.Combine(controlsDir, fileName);
            var header = BuildHeader(timestamp, "ControlObject analysis", control.Confidence * 100.0);
            var content = header + control.Code;
            await WriteFileAtomicAsync(path, content, ct);
            controlFiles.Add(fileName);
            _logger.LogTrace("Wrote control {File}", path);
        }

        // Pages — alphabetical by ClassName.
        var orderedPages = pages.OrderBy(p => p.ClassName, StringComparer.Ordinal).ToList();
        var pageFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var containerFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var containerCount = 0;

        foreach (var page in orderedPages)
        {
            ct.ThrowIfCancellationRequested();
            var fileName = $"{page.ClassName}.cs";
            var path = Path.Combine(pagesDir, fileName);
            var sourceLabel = $"Snapshot {page.SnapshotId} (Site {page.SiteId})";
            var confidence = page.Validation?.IsValid == true ? 100.0 : 0.0;
            var header = BuildHeader(timestamp, sourceLabel, confidence);
            var content = header + page.MainCode;
            await WriteFileAtomicAsync(path, content, ct);
            pageFiles.Add(fileName);
            _logger.LogTrace("Wrote page {File}", path);

            // Containers — alphabetical by full filename.
            var containerEntries = new List<(string FileName, string Code)>();
            foreach (var containerCode in page.ContainerCodes)
            {
                var match = ContainerClassRegex.Match(containerCode);
                if (!match.Success) continue;
                var containerClass = match.Groups[1].Value;
                var containerFileName = $"{page.ClassName}.{containerClass}.cs";
                containerEntries.Add((containerFileName, containerCode));
            }

            foreach (var (containerFileName, code) in containerEntries.OrderBy(e => e.FileName, StringComparer.Ordinal))
            {
                ct.ThrowIfCancellationRequested();
                var path2 = Path.Combine(containersDir, containerFileName);
                var header2 = BuildHeader(timestamp, sourceLabel, confidence);
                await WriteFileAtomicAsync(path2, header2 + code, ct);
                containerFiles.Add(containerFileName);
                containerCount++;
                _logger.LogTrace("Wrote container {File}", path2);
            }
        }

        // Cleanup orphans in Controls/, Pages/, Pages/Containers/.
        DeleteOrphans(controlsDir, controlFiles);
        DeleteOrphansShallow(pagesDir, pageFiles);
        DeleteOrphans(containersDir, containerFiles);

        _logger.LogInformation(
            "Wrote project to {OutputPath}: {ControlsCount} controls, {PagesCount} pages, {ContainersCount} containers",
            fullPath, orderedControls.Count, orderedPages.Count, containerCount);
    }

    private static string BuildHeader(string timestamp, string source, double confidencePercent)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.Append("//   Generated by Brinell.Scraper on ").AppendLine(timestamp);
        sb.Append("//   Source: ").AppendLine(source);
        sb.Append("//   Confidence: ").Append(confidencePercent.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("%");
        sb.AppendLine("//   Do not edit manually — regenerate from the scraper UI.");
        sb.AppendLine("// </auto-generated>");
        return sb.ToString();
    }

    private static async Task WriteFileAtomicAsync(string path, string content, CancellationToken ct)
    {
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    private static void DeleteOrphans(string directory, HashSet<string> keep)
    {
        if (!Directory.Exists(directory)) return;
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            if (!keep.Contains(name))
            {
                try { File.Delete(file); } catch { /* best effort */ }
            }
        }
        // Also clean stray .tmp files.
        foreach (var tmp in Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly))
        {
            try { File.Delete(tmp); } catch { /* best effort */ }
        }
    }

    private static void DeleteOrphansShallow(string directory, HashSet<string> keep)
    {
        // Pages/ — only top-level .cs files; do NOT recurse into Containers/.
        DeleteOrphans(directory, keep);
    }
}
