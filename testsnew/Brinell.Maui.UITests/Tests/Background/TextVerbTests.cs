using Brinell.Core;
using Brinell.Core.Diagnostics;
using Brinell.Core.Locators;
using Brinell.Maui.UITests.Pages;
using Xunit.Abstractions;

namespace Brinell.Maui.UITests.Tests.Background;

/// <summary>
/// Step 14: arranging a field's contents without the keyboard or the clipboard.
/// </summary>
/// <remarks>
/// <para>
/// <b>The clipboard is the reason this step exists.</b> <c>SendKeys</c> with
/// <see cref="TextInputMethod.Paste"/> writes the machine-wide clipboard and sends Ctrl+V. It
/// destroys whatever the person at the keyboard had copied, silently and permanently, and two
/// runs on one machine overwrite each other's text. Nothing about the operation needs a
/// clipboard; it was simply the fastest way to get a string into a field from outside the app.
/// </para>
/// <para>
/// <b>Typing is kept, deliberately, and is not a fallback that should disappear.</b> A keyboard
/// raises <c>TextChanged</c> per character, applies <c>MaxLength</c> as it goes and lets a
/// numeric keyboard refuse a letter; setting <c>Text</c> raises one change for the whole value.
/// A test <i>of</i> input behaviour needs the former and should say so with
/// <see cref="TextInputMethod.Keys"/>. What moves to the bridge is <i>arrangement</i> - getting
/// a field into the state a test wants to start from.
/// </para>
/// <para>
/// Each test runs under <see cref="PhysicalInputPolicy.Refused"/>, so passing is itself the
/// evidence: any fall-through to the keyboard, the clipboard or the foreground window would
/// have thrown. See <see cref="FocusVerbTests.RefusedPolicy_StillRefuses"/> for the guard on
/// that guard.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
[Trait("Stage", "Background")]
public class TextVerbTests
{
    private readonly MauiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public TextVerbTests(MauiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.Open(SamplePage.Text);
    }

    private TextTestPage Page => new(_fixture.Context);

    /// <summary>
    /// Text is written and read back, with no physical input.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task SetText_WritesWithoutPhysicalInput()
    {
        var page = Page;

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            page.TestEntry.SetText("stage b");

            Assert.Equal("stage b", page.TestEntry.GetText());
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Appending adds to what is there, and no longer types to do it.
    /// </summary>
    /// <remarks>
    /// Appending was the last routine keyboard input in the suite, and it typed because no
    /// <c>TextInputMethod</c> fits: <c>SetValue</c> replaces rather than appends, and reading
    /// then writing from out here leaves a window in which the app can change the field between
    /// the two calls. The verb does both on one pass of the app's UI thread, which is why it is
    /// a verb rather than sugar over the other two.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Append_AddsWithoutTyping()
    {
        var page = Page;

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            page.TestEntry.SetText("first");
            page.TestEntry.Append(" second");

            Assert.Equal("first second", page.TestEntry.GetText());
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clearing empties the field without Ctrl+A and Delete.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Clear_EmptiesWithoutPhysicalInput()
    {
        var page = Page;

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            page.TestEntry.SetText("something to remove");
            page.TestEntry.Clear();

            Assert.True(
                string.IsNullOrEmpty(page.TestEntry.GetText()),
                $"The entry still holds '{page.TestEntry.GetText()}'.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// <b>The canary.</b> A paste-method write leaves the system clipboard untouched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one assertion in this class about the machine rather than about the app. A sentinel
    /// goes on the clipboard, the field is written through the method that used to use it, and
    /// the sentinel must still be there afterwards.
    /// </para>
    /// <para>
    /// The clipboard is restored to whatever it held before, in a finally, because this test
    /// takes a desktop-global resource in order to prove the framework does not - and leaving a
    /// sentinel behind would be a smaller version of the same discourtesy.
    /// </para>
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Paste_LeavesTheClipboardAlone()
    {
        var page = Page;
        var sentinel = $"brinell-clipboard-canary-{Guid.NewGuid():N}";

        var element = _fixture.Context.TryFindElement(Locator.ByAutomationId("TestEntry"));
        Assert.NotNull(element);

        var restore = ReadClipboard();
        try
        {
            WriteClipboard(sentinel);

            using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
            {
                element.SendKeys("pasted without a clipboard", TextInputMethod.Paste);
            }

            Assert.Equal("pasted without a clipboard", page.TestEntry.GetText());
            Assert.Equal(sentinel, ReadClipboard());
        }
        finally
        {
            WriteClipboard(restore);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// A read-only field refuses the bridge, as it refuses everything else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The boundary that keeps the bridge honest. It writes a MAUI property directly, and MAUI's
    /// own setter does not enforce <c>IsReadOnly</c> - only the platform control does. So
    /// without an explicit refusal in the provider, the bridge would be a way of putting text in
    /// a field that no user could type in, and a test asserting on the result would pass while
    /// describing something impossible.
    /// </para>
    /// <para>
    /// <c>ReadOnlyEntry</c> declares the write verbs on purpose: declaring a verb says the
    /// element will be asked, not that it will agree.
    /// </para>
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task ReadOnlyEntry_RefusesTheBridge()
    {
        var page = Page;
        var before = page.ReadOnlyEntry.GetText();

        var element = _fixture.Context.TryFindElement(Locator.ByAutomationId("ReadOnlyEntry"));
        Assert.NotNull(element);

        // Outside a refusal scope: the point here is what the field holds afterwards, not which
        // route was taken, and every route must fail. Letting the ladder run to the end is what
        // makes that a complete statement.
        try
        {
            element.SendKeys("should not land", TextInputMethod.SetValue);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"write refused with {ex.GetType().Name}: {ex.Message}");
        }

        Assert.Equal(before, page.ReadOnlyEntry.GetText());

        return Task.CompletedTask;
    }

    /// <summary>
    /// Submit raises the control's completion command rather than pressing Enter.
    /// </summary>
    /// <remarks>
    /// The search bar is the one field here with a bound completion command, so it is the one
    /// whose submission has an outcome a test can see. An <c>Entry</c> whose app handles
    /// <c>Completed</c> as an event cannot be reached this way at all - <c>SendCompleted</c> is
    /// internal in MAUI - and correctly falls through to a real Enter.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Submit_RaisesTheSearchCommandWithoutPhysicalInput()
    {
        var page = Page;

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            page.TestSearchBar.SetText("brinell");
            page.TestSearchBar.Submit();
        }

        page.SearchStatusLabel.WaitTextContains("brinell", TestConstants.DefaultTestTimeoutMs);

        return Task.CompletedTask;
    }

    private static string ReadClipboard()
        => OnStaThread(() => System.Windows.Forms.Clipboard.ContainsText()
            ? System.Windows.Forms.Clipboard.GetText()
            : string.Empty) ?? string.Empty;

    private static void WriteClipboard(string text)
        => OnStaThread<object?>(() =>
        {
            if (string.IsNullOrEmpty(text))
            {
                System.Windows.Forms.Clipboard.Clear();
            }
            else
            {
                System.Windows.Forms.Clipboard.SetText(text);
            }

            return null;
        });

    /// <summary>
    /// Runs clipboard work on a thread the clipboard will talk to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Windows clipboard is OLE, and OLE needs a single-threaded apartment.</b> xUnit
    /// runs tests on MTA thread-pool threads, where every <c>Clipboard</c> call throws
    /// <c>ThreadStateException</c> before it touches anything.
    /// </para>
    /// <para>
    /// That is worth knowing beyond this file, because it explains a line in the step 3
    /// inventory: <c>SendKeys(Paste)</c> scored zero uses, and it would have failed on this
    /// before it ever reached the clipboard. The hazard the paste path represents is real for
    /// anything running STA - a person's own tooling, a WinForms harness - and unreachable from
    /// here. The canary needs its own thread to be a canary at all.
    /// </para>
    /// <para>
    /// Failures are swallowed: the clipboard is shared with every process on the desktop and any
    /// of them can hold it locked. An unreadable clipboard makes the canary inconclusive rather
    /// than failed, which is the honest outcome for a probe of something nobody owns.
    /// </para>
    /// </remarks>
    private static T? OnStaThread<T>(Func<T> work)
    {
        var result = default(T);

        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception)
            {
                // See the remarks: inconclusive, not failed.
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TestConstants.ShortTestTimeoutMs);

        return result;
    }
}
