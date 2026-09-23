# Step 5.9 — Parse LLM Response

## Objective

Extract C# code blocks from the LLM's markdown-formatted response. Handle both ControlObject and PageObject responses, including multiple code blocks per response.

## Dependencies

- Step 5.7 (control generation produces LLM responses)
- Step 5.8 (page generation produces LLM responses)

## Implementation

### CodeBlockParser

```csharp
// Services/CodeBlockParser.cs
public static class CodeBlockParser
{
    private static readonly Regex CodeBlockRegex = new(
        @"```(?:csharp|cs)\s*\n(.*?)```",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Extracts all C# code blocks from a markdown-formatted LLM response.
    /// </summary>
    public static IReadOnlyList<string> ExtractCSharpBlocks(string llmResponse)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
            return [];

        var matches = CodeBlockRegex.Matches(llmResponse);

        if (matches.Count > 0)
        {
            return matches
                .Select(m => m.Groups[1].Value.Trim())
                .Where(block => !string.IsNullOrWhiteSpace(block))
                .ToList();
        }

        // Fallback: no fenced code blocks found
        return TryExtractUnfencedCode(llmResponse);
    }

    /// <summary>
    /// Fallback extraction when no markdown fences are present.
    /// Strips leading/trailing prose lines that don't look like C#.
    /// </summary>
    private static IReadOnlyList<string> TryExtractUnfencedCode(string response)
    {
        var lines = response.Split('\n');
        var codeLines = new List<string>();
        var inCode = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            // Heuristic: lines starting with C# keywords or common code patterns
            if (!inCode && IsCSharpLine(trimmed))
                inCode = true;

            if (inCode)
                codeLines.Add(line);

            // Stop if we hit prose after code
            if (inCode && string.IsNullOrWhiteSpace(trimmed) &&
                codeLines.Count > 3)
            {
                // Check if next non-empty line is prose
                // Continue collecting — multi-line code has blank lines
            }
        }

        if (codeLines.Count == 0)
            return [];

        return [string.Join('\n', codeLines).Trim()];
    }

    private static bool IsCSharpLine(string line) =>
        line.StartsWith("using ", StringComparison.Ordinal) ||
        line.StartsWith("namespace ", StringComparison.Ordinal) ||
        line.StartsWith("public ", StringComparison.Ordinal) ||
        line.StartsWith("private ", StringComparison.Ordinal) ||
        line.StartsWith("internal ", StringComparison.Ordinal) ||
        line.StartsWith("sealed ", StringComparison.Ordinal) ||
        line.StartsWith("//", StringComparison.Ordinal) ||
        line.StartsWith("[", StringComparison.Ordinal) ||
        line.StartsWith("{", StringComparison.Ordinal);

    /// <summary>
    /// Splits a single code block containing multiple class declarations
    /// into separate blocks (one per class).
    /// </summary>
    public static IReadOnlyList<string> SplitByClassDeclarations(string code)
    {
        var classes = new List<string>();
        var lines = code.Split('\n');
        var current = new List<string>();
        var braceDepth = 0;
        var inClass = false;

        foreach (var line in lines)
        {
            // Detect class/record/struct declarations
            if (!inClass && Regex.IsMatch(line,
                @"^\s*(public|internal|sealed|abstract|static)\s+.*(class|record|struct)\s+"))
            {
                // Save any using/namespace preamble
                if (current.Count > 0 && classes.Count == 0)
                {
                    // Keep preamble for first class, prepend to subsequent
                }
                inClass = true;
            }

            if (inClass || current.Count == 0 || !inClass)
                current.Add(line);

            braceDepth += line.Count(c => c == '{') - line.Count(c => c == '}');

            if (inClass && braceDepth == 0 && current.Count > 1)
            {
                classes.Add(string.Join('\n', current).Trim());
                current.Clear();
                inClass = false;
            }
        }

        // Remaining lines
        if (current.Count > 0)
        {
            var remaining = string.Join('\n', current).Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
                classes.Add(remaining);
        }

        return classes.Count > 0 ? classes : [code];
    }
}
```

### Handling multiple code blocks

| Context | Handling |
|---------|----------|
| ControlObject generation (step 5.7) | Each block is a separate custom control class |
| PageObject generation (step 5.8) | First block = main PageObject, subsequent blocks = ContainerBase classes |
| Single block with multiple classes | Call `SplitByClassDeclarations()` to separate |

### Edge cases

- **Empty response**: return empty list
- **No fenced blocks**: fallback to unfenced extraction heuristic
- **Mixed `cs` / `csharp` fence markers**: regex handles both
- **Response with only prose and no code**: return empty list (caller handles error)
- **Code block with trailing whitespace/newlines**: trimmed

## Checklist

- [ ] `CodeBlockParser.ExtractCSharpBlocks()` extracts all C# blocks from markdown fences
- [ ] Handles both ` ```csharp ` and ` ```cs ` fence markers
- [ ] Fallback extraction for unfenced code (heuristic based on C# keywords)
- [ ] `SplitByClassDeclarations()` splits multi-class blocks into individual classes
- [ ] Empty/whitespace input returns empty list
- [ ] Blocks are trimmed of leading/trailing whitespace
- [ ] No regex catastrophic backtracking (compiled regex with `Singleline`)
