using Flainel.Specs;

// flainel check <spec or folder>...: every *.spec.md checked against docs/spec-format.md; exit 1 on a finding.
if (args is not ["check", _, ..])
{
    Console.Error.WriteLine("Usage: flainel check <spec.md or folder>...");
    return 2;
}

var files = args.Skip(1)
    .SelectMany<string, string>(path => Directory.Exists(path)
        ? Directory.EnumerateFiles(path, "*.spec.md", SearchOption.AllDirectories).Order()
        : [path])
    .ToList();

if (files.Count == 0)
{
    Console.Error.WriteLine("No *.spec.md found.");
    return 2;
}

var failed = 0;
foreach (var file in files)
{
    if (!File.Exists(file))
    {
        Console.Error.WriteLine($"{file}: not found");
        failed++;
        continue;
    }

    var findings = SpecCheck.CheckFile(file);
    Console.WriteLine(findings.Count == 0 ? $"{file}: follows the format" : $"{file}: {findings.Count} finding(s)");
    foreach (var finding in findings)
    {
        Console.WriteLine($"  {finding}");
    }

    failed += findings.Count > 0 ? 1 : 0;
}

return failed == 0 ? 0 : 1;
