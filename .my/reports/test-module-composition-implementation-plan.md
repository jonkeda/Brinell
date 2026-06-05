# Test Module Composition Implementation Plan

## Goal

Implement one Brinell composition model for UI tests and UAT:

- one fixture-level composition handle,
- automatic discovery of pages, flows, scenario services, and UAT phrase
  classes,
- DI construction and scoped lifetimes,
- UAT phrase metadata discovered from phrase classes,
- existing fixture-property UAT behavior kept as compatibility.

## Public Handle

```csharp
public TestComposition Composition { get; }
```

`TestComposition` is the discovered DI composition for the fixture.

## Current Code Reality

Current UAT runtime is fixture-property based:

- `srcnew/Brinell.Uat/UatRuntime.cs`
  creates `UatReflectionRuntime.FromRoot(root)`.
- `srcnew/Brinell.Uat/UatReflectionRuntime.cs`
  discovers public page properties on the fixture/root object.
- `srcnew/Brinell.Uat/UatDiscovery.cs`
  discovers pages/controls from assemblies, but phrase commands only from
  `[UatPhrase]` methods.
- `srcnew/Brinell.Uat/UatSpecFormatTestBase.cs`
  registers root phrase methods from `RuntimeRootType`.
- Existing UAT fixtures manually construct pages as public properties.

Compatibility must remain during migration.

## Proposed Public API

```csharp
[TestModuleScan(typeof(MyFixture), NamespacePrefix = "MyApp.UAT.Runtime")]
public sealed class MyFixture
{
    public MyFixture()
    {
        Composition = TestComposition.ForFixture(this, services =>
        {
            services.AddSingleton(Context);
            services.AddSingleton(Settings);
        });
    }

    public TestComposition Composition { get; }
}
```

```csharp
using var scope = fixture.Composition.CreateScope();
var flow = scope.ServiceProvider.GetRequiredService<LoginFlow>();
var page = scope.ServiceProvider.GetRequiredService<LoginPage>();
```

## Phase 1: Core Composition

Add to `srcnew/Brinell.Core`:

- `TestModuleScanAttribute`
- `TestPageAttribute`
- `TestScenarioServiceAttribute`
- `TestScenarioServiceBase`
- `TestComposition`
- `TestCompositionCatalog`
- `TestPageDescriptor`
- `TestServiceDescriptor`
- discovery options and diagnostics

Add package reference:

- `Microsoft.Extensions.DependencyInjection`

Core behavior:

- Read `[TestModuleScan]` from fixture type.
- Scan only selected assemblies and namespace prefixes.
- Discover PageObjects from `[TestPage]` or known Brinell page base classes.
- Discover `[TestScenarioService]` classes.
- Register fixture/root object as singleton.
- Register discovered PageObjects as scoped.
- Register discovered scenario services as scoped.
- Register caller-provided services through the configure callback.
- Build one root `IServiceProvider`.
- Create one `IServiceScope` per test/UAT scenario.

## Phase 2: UAT Phrase Classes

Add to `srcnew/Brinell.Uat`:

- `UatPhraseClassAttribute`
- `UatPhraseClassBase`
- phrase-class metadata discovery

Discovery rules:

- Discover `[UatPhraseClass]` or `UatPhraseClassBase` types from the same
  `TestComposition` scan.
- Register phrase classes as scoped DI services.
- Register public instance methods declared on phrase classes as phrase
  candidates.
- Prefer `[UatPhrase]` when present.
- Otherwise infer `Given`, `When`, `Then` phrases from method names.
- `[UatIgnore]` opts a public method out.
- Static methods, property accessors, and inherited `object` methods are
  ignored.

## Phase 3: UAT Runtime Integration

Update `srcnew/Brinell.Uat/UatReflectionRuntime.cs` and
`srcnew/Brinell.Uat/UatRuntime.cs`:

- Prefer `fixture.Composition` when available.
- Build built-in page/control commands from discovered page metadata.
- Build phrase command catalog from phrase-class metadata.
- For execution, create/receive an active DI scope and resolve the phrase class
  from that scope.
- Keep current fixture page-property discovery as compatibility.
- Keep current fixture `[UatPhrase]` method discovery as compatibility.

Update `srcnew/Brinell.Uat/UatScenarioExecutor.cs`:

- Allow execution with an existing `IServiceScope`.
- Ensure one scope per UAT scenario.

Update `srcnew/Brinell.Uat/UatScenarioTestBase.cs`:

- If fixture exposes `Composition`, create a scope per scenario.
- Otherwise use the existing root reflection path.

Update `srcnew/Brinell.Uat/UatSpecFormatTestBase.cs`:

- Bind phrase-class metadata without constructing phrase instances.
- Keep `RuntimeRootType` fixture phrase registration as compatibility.

## Phase 4: Tests

Add focused tests in `testsnew/Brinell.Uat.Tests`:

- composition discovers PageObjects from page base classes,
- composition discovers `[TestPage]` names,
- composition registers pages as scoped,
- composition registers flows/scenario services as scoped,
- tests can resolve pages directly from a scope,
- tests can resolve flows directly from a scope,
- tests can resolve scenario services from a scope,
- UAT discovers `[UatPhraseClass]` methods by convention,
- UAT honors `[UatPhrase]` on phrase-class methods,
- `[UatIgnore]` excludes public phrase-class helpers,
- duplicate phrase diagnostics include class/method/keyword,
- fixture page-property discovery still works,
- fixture `[UatPhrase]` methods still work,
- typed settings injection still works.

Run at minimum:

```powershell
dotnet test testsnew/Brinell.Uat.Tests/Brinell.Uat.Tests.csproj
dotnet test testsnew/Brinell.Core.Tests/Brinell.Core.Tests.csproj
```

## Phase 5: Representative Refactor

Refactor one existing UAT test project first, not all at once.

Recommended first target:

- `testsnew/Brinell.Html.Uat.Tests`

Why:

- lightweight host,
- existing fixture-owned pages,
- lower platform friction than MAUI/Presenter.

Refactor:

- add `[TestModuleScan]` to fixture,
- expose `Composition`,
- remove public page properties only if compatibility tests pass,
- add at least one flow,
- add at least one phrase class if needed.

Then consider:

- `testsnew/Brinell.WinForms.Uat.Tests`
- `testsnew/Brinell.Presenter.Uat.Tests`
- BodyCam UAT fixture

## Phase 6: Docs

Update:

- `docs/guides/uat-template-guide.md`
- `docs/guides/test-writing-guide.md`
- `docs/guides/best-practices.md`
- `docs/upgrade/uat-reporting-and-settings-upgrade.md`
- `docs/ai-assistant-references.md`
- `AGENTS.md`
- `.github/copilot-instructions.md`

Docs should say:

- no page catalog classes,
- no fixture page properties for new code,
- pages are auto-discovered,
- pages expose typed ControlObjects,
- tests may use pages directly,
- scenarios are optional,
- flows are reusable,
- phrase classes are thin UAT adapters,
- one composition handle owns DI scopes and metadata.

## Stop Points

Stop for review after:

1. API naming decision.
2. Core composition API compiles.
3. UAT phrase class metadata tests pass.
4. First existing UAT test project is refactored.

Do not refactor every platform test project in one pass.
