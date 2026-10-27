using Frainet.Specs;

// frainet check <spec or folder>...: every *.spec.md checked against docs/spec-format.md; exit 1 on a finding.
// frainet trace <spec> <tests folder>...: every rule of the spec reached by a test; exit 1 on a finding.
return args switch
{
    ["check", _, ..] => Check(args[1..]),
    ["trace", _, _, ..] => Trace(args[1], args[2..]),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("Usage: frainet check <spec.md or folder>...");
    Console.Error.WriteLine("       frainet trace <spec.md> <tests folder>...");
    return 2;
}

static int Check(string[] paths)
{
    var files = paths
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
}

static int Trace(string spec, string[] folders)
{
    foreach (var missing in folders.Prepend(spec).Where(path => !File.Exists(path) && !Directory.Exists(path)))
    {
        Console.Error.WriteLine($"{missing}: not found");
        return 2;
    }

    var result = RuleTrace.TraceFile(spec, folders);
    foreach (var (rule, reaches) in result.Rules)
    {
        Console.WriteLine($"{rule}: {(reaches.Count == 0 ? "no test" : $"{reaches.Count} test(s)")}");
        foreach (var reach in reaches)
        {
            Console.WriteLine($"  {reach.Name} ({reach.File}:{reach.Line})");
        }
    }

    var reached = result.Rules.Count(rule => rule.Value.Count > 0);
    Console.WriteLine($"{reached} of {result.Rules.Count} rules reached by a test, {result.Findings.Count} finding(s)");
    foreach (var finding in result.Findings)
    {
        Console.WriteLine($"  {finding}");
    }

    return result.Findings.Count == 0 ? 0 : 1;
}
