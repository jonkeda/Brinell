namespace Bruinell.Uia.Tests;

/// <summary>
/// Step 8: pins the parts of the contract that cannot change without breaking a deployed app.
/// </summary>
/// <remarks>
/// <para>
/// These are not tests of behaviour. They are tests that particular constants still have
/// particular values, because those values cross a process boundary between two assemblies
/// that are built and deployed separately. A refactor that "tidies" a GUID or reorders an enum
/// compiles, passes every behavioural test, and breaks every app under test that has not been
/// rebuilt - and does so silently, since the symptom is a pattern that is simply not there.
/// </para>
/// <para>
/// So the values are written out literally below rather than referenced. A test that asserted
/// <c>PatternGuid == BruinellUiaIds.PatternGuid</c> would pass no matter what anyone changed it
/// to, which is exactly the failure it is meant to catch.
/// </para>
/// </remarks>
[Trait("Category", "Uia")]
public class ContractTests
{
    /// <summary>The pattern GUID is the contract between the two processes.</summary>
    [Fact]
    public void PatternGuid_IsUnchanged()
    {
        Assert.Equal(
            new Guid("e1ff558b-b42c-400f-9597-d3ed8d84fda4"),
            BruinellUiaIds.PatternGuid);
    }

    /// <summary>The two interface ids are registered with the pattern and are equally frozen.</summary>
    [Fact]
    public void InterfaceIds_AreUnchanged()
    {
        Assert.Equal("d890790c-64e5-47fb-8539-c1a73c307c46", BruinellUiaIds.ProviderInterfaceIid);
        Assert.Equal("1a5456c2-a3a3-4175-9679-ec46b85915a8", BruinellUiaIds.ClientInterfaceIid);
    }

    /// <summary>
    /// The provider and client interfaces must not share an id.
    /// </summary>
    /// <remarks>
    /// They have identical members, so the obvious tidy-up is to collapse them into one type.
    /// UI Automation takes both ids at registration and treats them as different things; giving
    /// it the same id twice is not something it complains about at registration time.
    /// </remarks>
    [Fact]
    public void ProviderAndClientInterfaces_AreDistinct()
    {
        Assert.NotEqual(BruinellUiaIds.ProviderInterfaceIid, BruinellUiaIds.ClientInterfaceIid);
        Assert.NotEqual(
            typeof(IBruinellAutomationProvider).GUID, typeof(IBruinellAutomationPattern).GUID);
    }

    /// <summary>
    /// The declared interfaces carry the ids the contract says they do.
    /// </summary>
    /// <remarks>
    /// The constants and the attributes are two separate statements of the same fact, and the
    /// registration uses the constants while UI Automation's QueryInterface uses the attributes.
    /// If they drift, registration succeeds and no element ever appears to carry the pattern.
    /// </remarks>
    [Fact]
    public void DeclaredInterfaces_CarryTheContractIds()
    {
        Assert.Equal(
            new Guid(BruinellUiaIds.ProviderInterfaceIid), typeof(IBruinellAutomationProvider).GUID);
        Assert.Equal(
            new Guid(BruinellUiaIds.ClientInterfaceIid), typeof(IBruinellAutomationPattern).GUID);
    }

    /// <summary>
    /// The method table has exactly two entries, in this order.
    /// </summary>
    /// <remarks>
    /// UI Automation dispatches by index. Inserting a method renumbers every method after it,
    /// so a client built before the insertion calls the wrong one on a provider built after -
    /// with arguments that may even unmarshal cleanly.
    /// </remarks>
    [Fact]
    public void MethodTable_IsTwoMethodsInThisOrder()
    {
        var methods = typeof(IBruinellAutomationProvider).GetMethods();

        Assert.Equal(2, methods.Length);
        Assert.Equal(nameof(IBruinellAutomationProvider.Invoke), methods[0].Name);
        Assert.Equal(nameof(IBruinellAutomationProvider.Exchange), methods[1].Name);
    }

    /// <summary>The client interface mirrors the provider interface method for method.</summary>
    [Fact]
    public void ClientInterface_MirrorsTheProviderInterface()
    {
        var provider = typeof(IBruinellAutomationProvider).GetMethods();
        var client = typeof(IBruinellAutomationPattern).GetMethods();

        Assert.Equal(provider.Length, client.Length);

        for (var i = 0; i < provider.Length; i++)
        {
            Assert.Equal(provider[i].Name, client[i].Name);
            Assert.Equal(
                provider[i].GetParameters().Select(p => p.ParameterType.Name),
                client[i].GetParameters().Select(p => p.ParameterType.Name));
        }
    }

    /// <summary>
    /// Verb numbers that have shipped keep their numbers.
    /// </summary>
    /// <remarks>
    /// Spot-checks the first value of each range rather than every verb: the risk being guarded
    /// against is a range being moved wholesale, which is what an "organise the enum" change
    /// does. An individual verb cannot move without moving its range or colliding inside it,
    /// and <see cref="VerbNumbers_AreUnique"/> catches the collision.
    /// </remarks>
    [Theory]
    [InlineData(BruinellVerb.GetCapabilities, 1)]
    [InlineData(BruinellVerb.Tap, 100)]
    [InlineData(BruinellVerb.SwipeLeft, 103)]
    [InlineData(BruinellVerb.Focus, 200)]
    [InlineData(BruinellVerb.SetText, 300)]
    [InlineData(BruinellVerb.ScrollTo, 400)]
    [InlineData(BruinellVerb.NavigateBack, 500)]
    [InlineData(BruinellVerb.GetState, 600)]
    [InlineData(BruinellVerb.SetDate, 700)]
    [InlineData(BruinellVerb.SelectIndex, 800)]
    [InlineData(BruinellVerb.CurrentAlert, 900)]
    public void Verb_HasItsWireValue(BruinellVerb verb, int expected)
    {
        Assert.Equal(expected, (int)verb);
    }

    /// <summary>No two verbs share a number, which would make one of them unreachable.</summary>
    [Fact]
    public void VerbNumbers_AreUnique()
    {
        var values = Enum.GetValues<BruinellVerb>().Select(v => (int)v).ToArray();

        Assert.Equal(values.Length, values.Distinct().Count());
    }

    /// <summary>Every verb sits inside a declared range, so range-based negotiation is total.</summary>
    [Fact]
    public void EveryVerb_BelongsToARange()
    {
        foreach (var verb in Enum.GetValues<BruinellVerb>())
        {
            if (verb == BruinellVerb.None)
            {
                continue;
            }

            Assert.True(
                BruinellVerbs.RangeOf((int)verb) != BruinellVerbRange.Unknown,
                $"{verb} = {(int)verb} is outside every declared range. Ranges are how a client "
                + "asks an older provider whether it supports a capability area at all.");
        }
    }

    /// <summary>
    /// Each verb travels on the method it has always travelled on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// As frozen as the numbers, and pinned here for the same reason. A verb sent on the wrong
    /// one of the contract's two methods reaches a provider that has never heard of it and comes
    /// back <c>UIA_E_NOTSUPPORTED</c> - which reads as "this element does not support that", a
    /// plausible answer pointing at the app rather than at the call.
    /// </para>
    /// <para>
    /// That is not hypothetical: <c>Submit</c> was sent on <c>Invoke</c> during step 14, was
    /// refused, and fell silently back to a real Enter keystroke. The test that caught it was
    /// looking for physical input, not for a routing mistake.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(BruinellVerb.Ping, BruinellVerbTransport.Invoke)]
    [InlineData(BruinellVerb.GetCapabilities, BruinellVerbTransport.Exchange)]
    [InlineData(BruinellVerb.Tap, BruinellVerbTransport.Invoke)]
    [InlineData(BruinellVerb.SwipeRight, BruinellVerbTransport.Invoke)]
    [InlineData(BruinellVerb.Focus, BruinellVerbTransport.Invoke)]
    [InlineData(BruinellVerb.Unfocus, BruinellVerbTransport.Invoke)]
    [InlineData(BruinellVerb.IsFocused, BruinellVerbTransport.Exchange)]
    [InlineData(BruinellVerb.SetText, BruinellVerbTransport.Exchange)]
    [InlineData(BruinellVerb.AppendText, BruinellVerbTransport.Exchange)]
    [InlineData(BruinellVerb.ClearText, BruinellVerbTransport.Exchange)]
    [InlineData(BruinellVerb.GetText, BruinellVerbTransport.Exchange)]
    [InlineData(BruinellVerb.Submit, BruinellVerbTransport.Exchange)]
    [InlineData(BruinellVerb.NavigateBack, BruinellVerbTransport.Invoke)]
    [InlineData(BruinellVerb.GetState, BruinellVerbTransport.Exchange)]
    public void Verb_TravelsOnItsOwnMethod(BruinellVerb verb, BruinellVerbTransport expected)
    {
        Assert.Equal(expected, BruinellVerbs.TransportFor(verb));
    }

    /// <summary>
    /// Every verb has a transport, including the ones no build implements yet.
    /// </summary>
    /// <remarks>
    /// The map falls through to <see cref="BruinellVerbTransport.Invoke"/>, so this cannot fail
    /// as written - which is the point of stating it. It is here so that a future verb carrying
    /// a string is a visible decision in the map rather than an accidental default, and so the
    /// list below is a checklist a reader can work through.
    /// </remarks>
    [Fact]
    public void EveryVerb_HasATransport()
    {
        foreach (var verb in Enum.GetValues<BruinellVerb>())
        {
            if (verb == BruinellVerb.None)
            {
                continue;
            }

            Assert.True(
                Enum.IsDefined(BruinellVerbs.TransportFor(verb)),
                $"{verb} has no transport.");
        }
    }

    /// <summary>
    /// Every verb is classified as a read, an act on its element, or an act on the app.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A checklist that fails when the vocabulary grows.</b> <c>KindOf</c> falls through to
    /// <c>ElementAction</c>, so a fall-through test could never fail; this compares the whole enum
    /// against lists written out by hand, so adding a verb without deciding what it is breaks here
    /// rather than quietly changing an accessibility report.
    /// </para>
    /// <para>
    /// The decision matters because it decides what step 29's audit calls a defect. Getting a
    /// read wrong puts a label on an accessibility backlog; getting an element action wrong takes
    /// a gesture-only control off it; getting an app action wrong fills the backlog with pages,
    /// which is what the first audit actually did.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryVerb_IsClassifiedByKind()
    {
        // Reads: answer a question, change nothing.
        BruinellVerb[] reads =
        [
            BruinellVerb.None,
            BruinellVerb.GetCapabilities,
            BruinellVerb.GetProtocolVersion,
            BruinellVerb.GetBounds,
            BruinellVerb.Ping,
            BruinellVerb.IsFocused,
            BruinellVerb.GetText,
            BruinellVerb.ScrollPosition,
            BruinellVerb.CurrentRoute,
            BruinellVerb.GetState,
            BruinellVerb.IsIdle,
            BruinellVerb.CurrentAlert,
        ];

        // App actions: real, but not done to the element carrying the verb.
        BruinellVerb[] appActions =
        [
            BruinellVerb.NavigateBack,
            BruinellVerb.NavigateTo,
            BruinellVerb.InvokeMenuItem,
            BruinellVerb.InvokeToolbarItem,
            BruinellVerb.DismissAlert,
            BruinellVerb.OpenFlyout,
            BruinellVerb.CloseFlyout,
        ];

        // Element actions: done to the element itself. The only kind the audit examines.
        BruinellVerb[] elementActions =
        [
            BruinellVerb.Tap, BruinellVerb.DoubleTap, BruinellVerb.LongPress,
            BruinellVerb.SwipeLeft, BruinellVerb.SwipeRight, BruinellVerb.SwipeUp,
            BruinellVerb.SwipeDown, BruinellVerb.Pan, BruinellVerb.Pinch,
            BruinellVerb.Focus, BruinellVerb.Unfocus,
            BruinellVerb.SetText, BruinellVerb.AppendText, BruinellVerb.ClearText,
            BruinellVerb.Submit,
            BruinellVerb.ScrollTo, BruinellVerb.ScrollToIndex,
            BruinellVerb.SetDate, BruinellVerb.SetTime,
            BruinellVerb.SelectIndex, BruinellVerb.SelectByText,
        ];

        var classified = reads.Concat(appActions).Concat(elementActions).ToHashSet();
        var missing = Enum.GetValues<BruinellVerb>().Where(verb => !classified.Contains(verb));

        Assert.True(
            !missing.Any(),
            "These verbs are in the enum but in none of this test's lists, so nobody has decided "
            + "what kind they are: " + string.Join(", ", missing) + ". KindOf will call them "
            + "element actions by default, which puts whatever declares them on the "
            + "accessibility backlog until someone says otherwise.");

        AssertKind(reads, BruinellVerbKind.Read);
        AssertKind(appActions, BruinellVerbKind.AppAction);
        AssertKind(elementActions, BruinellVerbKind.ElementAction);
    }

    private static void AssertKind(IEnumerable<BruinellVerb> verbs, BruinellVerbKind expected)
    {
        foreach (var verb in verbs)
        {
            Assert.True(
                BruinellVerbs.KindOf(verb) == expected,
                $"{verb} is a {BruinellVerbs.KindOf(verb)}, expected {expected}.");
        }
    }

    /// <summary>A verb number from a future build reports its range rather than failing.</summary>
    [Fact]
    public void RangeOf_ClassifiesVerbsThisBuildDoesNotKnow()
    {
        Assert.Equal(BruinellVerbRange.Text, BruinellVerbs.RangeOf(398));
        Assert.Equal(BruinellVerbRange.Gestures, BruinellVerbs.RangeOf(150));
        Assert.Equal(BruinellVerbRange.Unknown, BruinellVerbs.RangeOf(0));
        Assert.Equal(BruinellVerbRange.Unknown, BruinellVerbs.RangeOf(5000));
        Assert.Equal(BruinellVerbRange.Unknown, BruinellVerbs.RangeOf(-1));
    }

    /// <summary>Capabilities survive being written and read back.</summary>
    [Fact]
    public void Capabilities_RoundTrip()
    {
        BruinellVerb[] verbs = [BruinellVerb.SwipeLeft, BruinellVerb.Tap, BruinellVerb.SetText];

        var formatted = BruinellVerbs.FormatCapabilities(verbs);

        Assert.Equal("100,103,300", formatted);
        Assert.Equal(
            [BruinellVerb.Tap, BruinellVerb.SwipeLeft, BruinellVerb.SetText],
            BruinellVerbs.ParseCapabilities(formatted));
    }

    /// <summary>
    /// A capability list from a newer provider is read, not rejected.
    /// </summary>
    /// <remarks>
    /// The asymmetric half of the versioning story. A test assembly older than the app under
    /// test will be told about verbs it has no name for; dropping them and keeping the rest is
    /// what lets an old suite run against a new app.
    /// </remarks>
    [Fact]
    public void Capabilities_IgnoreEntriesThisBuildCannotName()
    {
        var verbs = BruinellVerbs.ParseCapabilities("100,4242,300,not-a-number,");

        Assert.Equal([BruinellVerb.Tap, BruinellVerb.SetText], verbs);
    }

    /// <summary>An empty or absent capability list is a list of nothing, not a failure.</summary>
    [Fact]
    public void Capabilities_HandleNothing()
    {
        Assert.Empty(BruinellVerbs.ParseCapabilities(null));
        Assert.Empty(BruinellVerbs.ParseCapabilities(string.Empty));
        Assert.Empty(BruinellVerbs.ParseCapabilities("   "));
        Assert.Equal(string.Empty, BruinellVerbs.FormatCapabilities([]));
    }

    /// <summary>
    /// Registration is idempotent, and safe to race.
    /// </summary>
    /// <remarks>
    /// The app under test may attach a bridge from more than one window, and the test assembly
    /// resolves the pattern from whatever thread the test runs on. Two threads arriving at once
    /// must produce one registration, not two rooted handlers and two ids.
    /// </remarks>
    [Fact]
    public void Registration_IsIdempotentUnderParallelThreads()
    {
        var results = new BruinellPatternRegistration[32];

        Parallel.For(0, results.Length, i => results[i] = BruinellPatternRegistration.Current);

        Assert.All(results, r => Assert.Same(results[0], r));
        Assert.All(results, r => Assert.Equal(results[0].PatternId, r.PatternId));
        Assert.True(results[0].PatternId > 0);
    }

    /// <summary>
    /// The pattern and its availability property get different ids.
    /// </summary>
    /// <remarks>
    /// They are allocated from the same sequence, so confusing one for the other yields a
    /// plausible-looking number that addresses the wrong thing.
    /// </remarks>
    [Fact]
    public void Registration_ProducesDistinctPatternAndPropertyIds()
    {
        var registration = BruinellPatternRegistration.Current;

        Assert.NotEqual(registration.PatternId, registration.IsPatternAvailablePropertyId);
    }

    /// <summary>Failure descriptions name the verb and say whether retrying is worthwhile.</summary>
    [Fact]
    public void Describe_SaysWhatToDoAboutIt()
    {
        var notSupported = BruinellVerbFailure.Describe(
            BruinellVerb.SwipeUp, HResults.UIA_E_NOTSUPPORTED);
        var gone = BruinellVerbFailure.Describe(
            BruinellVerb.Tap, HResults.UIA_E_ELEMENTNOTAVAILABLE);

        Assert.Contains("SwipeUp", notSupported, StringComparison.Ordinal);
        Assert.Contains("not succeed on retry", notSupported, StringComparison.Ordinal);

        Assert.Contains("Tap", gone, StringComparison.Ordinal);
        Assert.Contains("retry", gone, StringComparison.Ordinal);
    }

    /// <summary>Success and failure are told apart by sign, as COM defines it.</summary>
    [Fact]
    public void Succeeded_FollowsTheComConvention()
    {
        Assert.True(BruinellVerbFailure.Succeeded(HResults.S_OK));
        Assert.True(BruinellVerbFailure.Succeeded(HResults.S_FALSE));
        Assert.False(BruinellVerbFailure.Succeeded(HResults.UIA_E_NOTSUPPORTED));
        Assert.False(BruinellVerbFailure.Succeeded(HResults.E_FAIL));
    }
}
