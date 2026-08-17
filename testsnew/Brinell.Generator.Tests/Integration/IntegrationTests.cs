using Brinell.Generator.Models;

namespace Brinell.Generator.Tests.Integration;

public class IntegrationTests
{
    [Theory]
    [InlineData("SimpleClickableClass")]
    [InlineData("MultiMethodClass")]
    [InlineData("ControlBase")]
    [InlineData("MixedControl")]
    public void GeneratesExpectedOutput(string name)
    {
        // Arrange
        var inputPath = Path.Combine(GetTestDataDirectory(), "Input", $"{name}.input.cs");
        var expectedPath = Path.Combine(GetTestDataDirectory(), "Expected", $"{name}.expected.cs");
        var inputCode = File.ReadAllText(inputPath);
        var expectedCode = File.ReadAllText(expectedPath);

        var generator = ControlObjectGenerator.CreateDefault();
        var options = new GeneratorOptions { IncludeGeneratedHeader = true };

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
