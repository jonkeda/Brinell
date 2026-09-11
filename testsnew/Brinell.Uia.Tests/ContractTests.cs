namespace Brinell.Uia.Tests;

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
/// <c>PatternGuid == BrinellUiaIds.PatternGuid</c> would pass no matter what anyone changed it
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
            BrinellUiaIds.PatternGuid);
    }

    /// <summary>The two interface ids are registered with the pattern and are equally frozen.</summary>
    [Fact]
    public void InterfaceIds_AreUnchanged()
    {
        Assert.Equal("d890790c-64e5-47fb-8539-c1a73c307c46", BrinellUiaIds.ProviderInterfaceIid);
        Assert.Equal("1a5456c2-a3a3-4175-9679-ec46b85915a8", BrinellUiaIds.ClientInterfaceIid);
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
        Assert.NotEqual(BrinellUiaIds.ProviderInterfaceIid, BrinellUiaIds.ClientInterfaceIid);
        Assert.NotEqual(
            typeof(IBrinellAutomationProvider).GUID, typeof(IBrinellAutomationPattern).GUID);
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
            new Guid(BrinellUiaIds.ProviderInterfaceIid), typeof(IBrinellAutomationProvider).GUID);
        Assert.Equal(
            new Guid(BrinellUiaIds.ClientInterfaceIid), typeof(IBrinellAutomationPattern).GUID);
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
        var methods = typeof(IBrinellAutomationProvider).GetMethods();

        Assert.Equal(2, methods.Length);
        Assert.Equal(nameof(IBrinellAutomationProvider.Invoke), methods[0].Name);
        Assert.Equal(nameof(IBrinellAutomationProvider.Exchange), methods[1].Name);
    }

    /// <summary>The client interface mirrors the provider interface method for method.</summary>
    [Fact]
    public void ClientInterface_MirrorsTheProviderInterface()
    {
        var provider = typeof(IBrinellAutomationProvider).GetMethods();
        var client = typeof(IBrinellAutomationPattern).GetMethods();

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
    [InlineData(BrinellVerb.GetCapabilities, 1)]
    [InlineData(BrinellVerb.Tap, 100)]
    [InlineData(BrinellVerb.SwipeLeft, 103)]
    [InlineData(BrinellVerb.Focus, 200)]
    [InlineData(BrinellVerb.SetText, 300)]
    [InlineData(BrinellVerb.ScrollTo, 400)]
    [InlineData(BrinellVerb.NavigateBack, 500)]
    [InlineData(BrinellVerb.GetState, 600)]
    [InlineData(BrinellVerb.SetDate, 700)]
    [InlineData(BrinellVerb.SelectIndex, 800)]
    [InlineData(BrinellVerb.CurrentAlert, 900)]
    public void Verb_HasItsWireValue(BrinellVerb verb, int expected)
    {
        Assert.Equal(expected, (int)verb);
    }

    /// <summary>No two verbs share a number, which would make one of them unreachable.</summary>
    [Fact]
    public void VerbNumbers_AreUnique()
    {
        var values = Enum.GetValues<BrinellVerb>().Select(v => (int)v).ToArray();

        Assert.Equal(values.Length, values.Distinct().Count());
    }

    /// <summary>Every verb sits inside a declared range, so range-based negotiation is total.</summary>
    [Fact]
    public void EveryVerb_BelongsToARange()
    {
        foreach (var verb in Enum.GetValues<BrinellVerb>())
        {
            if (verb == BrinellVerb.None)
            {
                continue;
            }

            Assert.True(
                BrinellVerbs.RangeOf((int)verb) != BrinellVerbRange.Unknown,
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
    [InlineData(BrinellVerb.Ping, BrinellVerbTransport.Invoke)]
    [InlineData(BrinellVerb.GetCapabilities, BrinellVerbTransport.Exchange)]
    [InlineData(BrinellVerb.Tap, BrinellVerbTransport.Invoke)]
    [InlineData(BrinellVerb.SwipeRight, BrinellVerbTransport.Invoke)]
    [InlineData(BrinellVerb.Focus, BrinellVerbTransport.Invoke)]
    [InlineData(BrinellVerb.Unfocus, BrinellVerbTransport.Invoke)]
    [InlineData(BrinellVerb.IsFocused, BrinellVerbTransport.Exchange)]
    [InlineData(BrinellVerb.SetText, BrinellVerbTransport.Exchange)]
    [InlineData(BrinellVerb.AppendText, BrinellVerbTransport.Exchange)]
    [InlineData(BrinellVerb.ClearText, BrinellVerbTransport.Exchange)]
    [InlineData(BrinellVerb.GetText, BrinellVerbTransport.Exchange)]
    [InlineData(BrinellVerb.Submit, BrinellVerbTransport.Exchange)]
    [InlineData(BrinellVerb.NavigateBack, BrinellVerbTransport.Invoke)]
    [InlineData(BrinellVerb.GetState, BrinellVerbTransport.Exchange)]
    public void Verb_TravelsOnItsOwnMethod(BrinellVerb verb, BrinellVerbTransport expected)
    {
        Assert.Equal(expected, BrinellVerbs.TransportFor(verb));
    }

    /// <summary>
    /// Every verb has a transport, including the ones no build implements yet.
    /// </summary>
    /// <remarks>
    /// The map falls through to <see cref="BrinellVerbTransport.Invoke"/>, so this cannot fail
    /// as written - which is the point of stating it. It is here so that a future verb carrying
    /// a string is a visible decision in the map rather than an accidental default, and so the
    /// list below is a checklist a reader can work through.
    /// </remarks>
    [Fact]
    public void EveryVerb_HasATransport()
    {
        foreach (var verb in Enum.GetValues<BrinellVerb>())
        {
            if (verb == BrinellVerb.None)
            {
                continue;
            }

            Assert.True(
                Enum.IsDefined(BrinellVerbs.TransportFor(verb)),
                $"{verb} has no transport.");
        }
    }

    /// <summary>A verb number from a future build reports its range rather than failing.</summary>
    [Fact]
    public void RangeOf_ClassifiesVerbsThisBuildDoesNotKnow()
    {
        Assert.Equal(BrinellVerbRange.Text, BrinellVerbs.RangeOf(398));
        Assert.Equal(BrinellVerbRange.Gestures, BrinellVerbs.RangeOf(150));
        Assert.Equal(BrinellVerbRange.Unknown, BrinellVerbs.RangeOf(0));
        Assert.Equal(BrinellVerbRange.Unknown, BrinellVerbs.RangeOf(5000));
        Assert.Equal(BrinellVerbRange.Unknown, BrinellVerbs.RangeOf(-1));
    }

    /// <summary>Capabilities survive being written and read back.</summary>
    [Fact]
    public void Capabilities_RoundTrip()
    {
        BrinellVerb[] verbs = [BrinellVerb.SwipeLeft, BrinellVerb.Tap, BrinellVerb.SetText];

        var formatted = BrinellVerbs.FormatCapabilities(verbs);

        Assert.Equal("100,103,300", formatted);
        Assert.Equal(
            [BrinellVerb.Tap, BrinellVerb.SwipeLeft, BrinellVerb.SetText],
            BrinellVerbs.ParseCapabilities(formatted));
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
        var verbs = BrinellVerbs.ParseCapabilities("100,4242,300,not-a-number,");

        Assert.Equal([BrinellVerb.Tap, BrinellVerb.SetText], verbs);
    }

    /// <summary>An empty or absent capability list is a list of nothing, not a failure.</summary>
    [Fact]
    public void Capabilities_HandleNothing()
    {
        Assert.Empty(BrinellVerbs.ParseCapabilities(null));
        Assert.Empty(BrinellVerbs.ParseCapabilities(string.Empty));
        Assert.Empty(BrinellVerbs.ParseCapabilities("   "));
        Assert.Equal(string.Empty, BrinellVerbs.FormatCapabilities([]));
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
        var results = new BrinellPatternRegistration[32];

        Parallel.For(0, results.Length, i => results[i] = BrinellPatternRegistration.Current);

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
        var registration = BrinellPatternRegistration.Current;

        Assert.NotEqual(registration.PatternId, registration.IsPatternAvailablePropertyId);
    }

    /// <summary>Failure descriptions name the verb and say whether retrying is worthwhile.</summary>
    [Fact]
    public void Describe_SaysWhatToDoAboutIt()
    {
        var notSupported = BrinellVerbFailure.Describe(
            BrinellVerb.SwipeUp, HResults.UIA_E_NOTSUPPORTED);
        var gone = BrinellVerbFailure.Describe(
            BrinellVerb.Tap, HResults.UIA_E_ELEMENTNOTAVAILABLE);

        Assert.Contains("SwipeUp", notSupported, StringComparison.Ordinal);
        Assert.Contains("not succeed on retry", notSupported, StringComparison.Ordinal);

        Assert.Contains("Tap", gone, StringComparison.Ordinal);
        Assert.Contains("retry", gone, StringComparison.Ordinal);
    }

    /// <summary>Success and failure are told apart by sign, as COM defines it.</summary>
    [Fact]
    public void Succeeded_FollowsTheComConvention()
    {
        Assert.True(BrinellVerbFailure.Succeeded(HResults.S_OK));
        Assert.True(BrinellVerbFailure.Succeeded(HResults.S_FALSE));
        Assert.False(BrinellVerbFailure.Succeeded(HResults.UIA_E_NOTSUPPORTED));
        Assert.False(BrinellVerbFailure.Succeeded(HResults.E_FAIL));
    }
}
