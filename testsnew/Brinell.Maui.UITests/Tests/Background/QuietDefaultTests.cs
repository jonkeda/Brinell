using Brinell.Core.Diagnostics;
using Xunit;

namespace Brinell.Maui.UITests.Tests.Background;

/// <summary>
/// Step 49: with nothing set, a MAUI run through FlaUI is quiet.
/// </summary>
/// <remarks>
/// The end state of the quiet-run plan, pinned. It used to take <c>BRINELL_BACKGROUND_MODE=1</c>;
/// now silence means refused, and an explicit <c>0</c> is what asks for real input.
/// </remarks>
[Trait("Category", "UITest")]
[Trait("Stage", "Background")]
public class QuietDefaultTests
{
    [Fact]
    public void WithNothingSet_PhysicalInputIsRefused()
    {
        var asked = Environment.GetEnvironmentVariable("BRINELL_BACKGROUND_MODE");

        // Touching the driver type is what a real run does before any input decision.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(
            typeof(Brinell.Maui.FlaUI.FlaUIMauiDriver).Module.ModuleHandle);

        if (!string.IsNullOrWhiteSpace(asked))
        {
            // Somebody asked explicitly; the default is not what is in force, so there is
            // nothing to check about it in this run.
            return;
        }

        Assert.Equal(PhysicalInputPolicy.Refused, PhysicalInput.Policy);
    }
}
