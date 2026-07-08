using Brinell.Generator.Handlers;
using Brinell.Generator.Models;

namespace Brinell.Generator.Tests.Integration;

public class IntegrationTests
{
    [Fact]
    public void GeneratesCorrectOutput_ForSimpleClickableClass()
    {
        // Arrange
        var inputPath = Path.Combine(GetTestDataDirectory(), "Input", "SimpleClickableClass.input.cs");
        var expectedPath = Path.Combine(GetTestDataDirectory(), "Expected", "SimpleClickableClass.expected.cs");
        var inputCode = File.ReadAllText(inputPath);
        var expectedCode = File.ReadAllText(expectedPath);

        var generator = new MethodWrapperGenerator();
        var options = new GeneratorOptions
        {
            Handlers = new List<IMethodHandler> { new CoreMethodHandler() },
            IncludeGeneratedHeader = true
        };

        // Act
        var generatedCode = generator.Generate(inputCode, options);

        // Assert
        Assert.Equal(NormalizeCode(expectedCode), NormalizeCode(generatedCode));
    }

    [Fact]
    public void GeneratesCorrectOutput_ForMultiMethodClass()
    {
        // Arrange
        var inputPath = Path.Combine(GetTestDataDirectory(), "Input", "MultiMethodClass.input.cs");
        var expectedPath = Path.Combine(GetTestDataDirectory(), "Expected", "MultiMethodClass.expected.cs");
        var inputCode = File.ReadAllText(inputPath);
        var expectedCode = File.ReadAllText(expectedPath);

        var generator = new MethodWrapperGenerator();
        var options = new GeneratorOptions
        {
            Handlers = new List<IMethodHandler> { new CoreMethodHandler() },
            IncludeGeneratedHeader = true
        };

        // Act
        var generatedCode = generator.Generate(inputCode, options);

        // Assert
        Assert.Equal(NormalizeCode(expectedCode), NormalizeCode(generatedCode));
    }

    /// <summary>
    /// Normalizes code for comparison by removing insignificant whitespace.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        // Trim and normalize line endings
        var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var normalizedLines = lines
            .Select(l => l.TrimEnd())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        return string.Join("\n", normalizedLines);
    }

    private static string GetTestDataDirectory()
    {
        var testDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(testDir, "..", "..", "..", "TestData");
    }
}
