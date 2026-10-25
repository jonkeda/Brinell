using Bruinell.Core.Composition;
using Xunit;

namespace Bruinell.Core.Tests.Composition;

/// <summary>Where a suite was built, also when a runner runs it from a copy of its output.</summary>
public sealed class AssemblyFileTests
{
    [Fact]
    public void FolderOf_IsTheAssemblysFolder_OrTheOutputACopyWasMadeFrom()
    {
        var here = Path.GetDirectoryName(typeof(AssemblyFileTests).Assembly.Location)!;
        Assert.Equal(here, AssemblyFile.FolderOf(typeof(AssemblyFileTests).Assembly));

        var built = Path.Combine(Path.GetTempPath(), "built-output");
        Environment.SetEnvironmentVariable(AssemblyFile.BuiltOutputEnvironmentVariable, built);
        try
        {
            // This process's folder plays the copy: its assemblies map onto the output it came from.
            Assert.Equal(built, AssemblyFile.FolderOf(typeof(AssemblyFileTests).Assembly));

            // An assembly outside the copy (the runtime's) is where it is.
            var runtime = typeof(object).Assembly;
            Assert.Equal(Path.GetDirectoryName(runtime.Location), AssemblyFile.FolderOf(runtime));
        }
        finally
        {
            Environment.SetEnvironmentVariable(AssemblyFile.BuiltOutputEnvironmentVariable, null);
        }
    }
}
