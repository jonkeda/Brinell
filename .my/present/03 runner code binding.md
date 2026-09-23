# Runner Code Binding

Status: Delivered, with the vocabulary model superseded (2026-09)
Date: 2026-07-07, revised 2026-09-21
Area: `srcnew/Brinell.Uat`, `srcnew/Brinell.Core/Testing`
Related:

- [UAT phrases and flows](../../docs/guides/uat-phrases-and-flows.md) — the authoritative phrase list
- [The Gherkin language, formalised](../uat/attribute-catalog/01-grammar.md) — grammar, phrase mini-language, lexicon
- [Attribute-driven UAT catalog](../uat/attribute-catalog/README.md)
- [AD-011 — UAT vocabulary is attribute-declared](../../.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md)

This document defines how the UAT runner maps parsed Markdown steps to executable Brinell code.

> **Superseded (2026-09):** the binding *pipeline* below shipped as designed. What
> changed is **where the phrase vocabulary is declared**. The original design put a
> phrase on each concrete control method; the shipped design puts `[UatStep]` on the
> `Brinell.Core` control *interfaces* and discovers it, so there is no hand-written
> phrase table anywhere. Sections marked with this quote style carry the correction.

## Binding Goal

The runner executes only known UAT commands.

```text
Markdown step
  -> parsed step model
  -> command pattern match
  -> page/control resolution
  -> typed Brinell handler call
  -> result and diagnostics
```

UAT Markdown does not call arbitrary methods directly.

## Core Concepts

```text
UAT command
  A supported phrase pattern such as "I tap {control}".

Command handler
  Typed code that executes a matched command.

Page registry
  Maps canonical UAT page names to PageObject types.

Control registry
  Maps canonical UAT control names to ControlObject members on a page.

Capability
  A supported control action such as Tap, EnterText, Select, Check, or AssertValue.

Discovery config
  Runner configuration that tells the runner which assemblies to scan.
```

## Built-In Command Set

The built-in vocabulary is **18 phrases**, and it splits in two along the line that
matters for this document: three verbs the engine owns, and everything else,
which is discovered from the controls.

**Engine-owned page verbs** — registered by hand, once, in
`UatReflectionRuntime.CreateCommandCatalog()`. They are not control methods, so
they have nowhere else to live.

| Keyword | Phrase | Command id |
| --- | --- | --- |
| Given | `I am on the {page} page` | `Builtin.Page.Open` |
| Then | `I should be on the {page} page` | `Builtin.Page.AssertOpen` |
| Then | `I should see {text}` | `Builtin.Page.AssertTextVisible` |

**Discovered control verbs** — 15 phrases, none of them written in a catalog file.
Each is a `[UatStep]` attribute on a `Brinell.Core` control interface method; the
table below is a summary, and
[uat-phrases-and-flows.md](../../docs/guides/uat-phrases-and-flows.md) §Built-In
Phrases is the authoritative list.

| Declaring interface | Phrases |
| --- | --- |
| `IClickableControlObject<TScope>` | `I tap {control}` |
| `IEditableTextControlObject<TScope>` | `I enter {value} into {control}`, `I set {control} to {value}`, `I clear {control}` |
| `ITextControlObject<TScope>` | `{control} should equal {value}`, `{control} should contain {value}` |
| `IToggleControlObject<TScope>` | `I check {control}`, `I uncheck {control}`, `{control} should be checked`, `{control} should be unchecked` |
| `ISelectorControlObject<TScope>` | `I select {value} from {control}`, `{control} should have selected {value}` |
| `IElementObject<TScope>` | `{control} should be visible`, `{control} should not be visible`, `{control} should be enabled` |

> **Superseded (2026-09):** the original table on this line proposed five phrases
> that were never built — `the application is running`, `I enter {formName}`,
> `I select {value} in {control}` (shipped as `from`), `I should see {value} in
> {control}` and `I should see these {items}`. Table-driven form entry and list
> assertions are served by custom `[UatPhrase]` methods instead, which is why no
> built-in table handler was needed. `I should see the {page} page` shipped as
> `I should be on the {page} page`.

Projects add more phrases in one of two ways, both attribute-driven: `[UatStep]`
on an app control type (discovered alongside the Core ones), or `[UatPhrase]` on
a root or phrase-class method for anything that is not a single control action.

## Page And Control Names

The runner exposes one canonical UAT name per page and one canonical UAT name per control.

Aliases are not part of the first version.

Default names are inferred from type and member names:

```text
LoginPage              -> Login
CustomerDetailsPage    -> Customer Details
SignInButton           -> Sign In
UserNameInput          -> User Name
PasswordField          -> Password
RememberMeCheckbox     -> Remember Me
CountryPicker          -> Country
```

Suffixes stripped during name inference, as shipped in `UatNameInference`:

```text
Page      Button    Input     Entry     Field
TextBox   CheckBox  Checkbox  Switch    Toggle
Picker    Dropdown  List      Grid      Label
Display   Message   Text      Control
```

`[UatName]` overrides the inferred name:

```csharp
public class LoginPage : PageObjectBase<LoginPage>
{
    public LoginPage(IMauiTestContext context) : base(context) { }

    [UatName("Sign in")]
    public Button<LoginPage> SubmitButton => new(this, "Login_Submit");

    [UatName("User name")]
    public Entry<LoginPage> EmailInput => new(this, "Login_Email");
}
```

This is layer 3 of the language — the *lexicon* — and it is the one layer that was
already attribute-driven in the original design. See
[01-grammar.md](../uat/attribute-catalog/01-grammar.md) §3.

## Name Scope

Page names are global.

```text
Login       -> LoginPage
Customers   -> CustomersPage
```

Control names are scoped to a page.

```text
Login.Sign in       -> LoginPage.SubmitButton
Customers.Search    -> CustomersPage.SearchInput
```

Short control names resolve against the current page context.

```md
Given I am on the Login page
When I tap Sign in
```

If there is no current page, or if the control cannot be resolved on the current page, validation fails.

## Capability Binding

Generic commands bind only to controls with matching capabilities.

```text
I tap {control}
  requires Tap capability

I enter {value} into {control}
  requires EnterText capability

I select {value} from {control}
  requires Select capability

I check {control}
  requires Check capability

{control} should contain {value}
  requires readable text assertion capability
```

Capabilities are represented by the `Brinell.Core` control interfaces. That is not
an incidental detail any more: because the phrase is declared *on the interface
method*, a control gets the phrase exactly when it implements the interface that
declares it. Capability matching and phrase declaration became the same thing.

## UAT Attributes

Five attributes, and it is worth being precise about which layer each one serves.

| Attribute | Lives in | Declares |
| --- | --- | --- |
| `[UatStep]` | `Brinell.Core/Testing` | A built-in control phrase, on the interface method it calls |
| `[UatName]` | `Brinell.Uat` | The lexicon name of a page or control |
| `[UatPhrase]` | `Brinell.Uat` | A custom phrase on a root or phrase-class method |
| `[UatPhraseClass]` | `Brinell.Uat` | A type whose methods carry custom phrases |
| `[UatIgnore]` | `Brinell.Uat` | Excludes a type or member from discovery |

`[UatStep]` is the one the original design did not have, and it carries the whole
built-in vocabulary:

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatStepAttribute : Attribute
{
    public UatStepAttribute(UatEffectiveStepKeyword keyword, string phrase) { }

    public UatEffectiveStepKeyword Keyword { get; }
    public string Phrase { get; }

    /// Literal bool passed to the method for variants with no {value} placeholder.
    public bool Literal { get; init; }
    public bool HasLiteral { get; init; }

    /// Stable command-id *suffix*; defaults to the method name.
    public string? CommandId { get; init; }
}
```

Two details earn their keep:

- **`HasLiteral` / `Literal`** are how one method backs two phrases.
  `AssertVisible(bool?)` carries both `should be visible` (`Literal = true`) and
  `should not be visible` (`Literal = false`); the discovery pass turns each into
  its own catalog row with the bool baked in.
- **`CommandId` is a suffix, not a full id.** The attribute says
  `CommandId = "Control.SetText"`; the runtime projection prepends `Builtin.` and
  the spec projection prepends `Spec.`. One stable suffix, two projections, and a
  test can compare them on it.

The rest of the set is unchanged from the original design:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UatNameAttribute : Attribute
{
    public UatNameAttribute(string name) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatPhraseAttribute : Attribute
{
    public UatPhraseAttribute(string phrase) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatActionAttribute : Attribute
{
    public UatActionAttribute(string actionName) { }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UatIgnoreAttribute : Attribute
{
}
```

`[UatPhrase]` also has a second constructor taking the keyword —
`[UatPhrase(UatEffectiveStepKeyword.When, "I sign in with credentials")]` — which
is the form to prefer. Without it the phrase is registered for every keyword.

`[UatAction]` still exists and is still read by `UatDiscovery`, but note what it
is **not**: it marks a method as a capability for discovery purposes, and it does
not contribute a phrase. Phrases come from `[UatStep]` and `[UatPhrase]` only.

No `[UatAlias]` attribute exists.

## Where Control Phrases Are Declared

> **Superseded (2026-09):** this section originally showed `[UatAction]` +
> `[UatPhrase]` on concrete control classes (`ButtonControl.TapAsync`). That is
> not how it shipped. Phrases sit on the **`Brinell.Core` control interfaces**, so
> every technology's control — MAUI, WPF, WinForms, Blazor, HTML, Stride — inherits
> the same phrase by implementing the same interface, and no technology package
> repeats it.

A built-in verb, exactly as it appears in `Brinell.Core`:

```csharp
public interface IClickableControlObject<TScope> : IControlObject<TScope>
{
    [UatStep(UatEffectiveStepKeyword.When, "I tap {control}", CommandId = "Control.Tap")]
    TScope Click(int? timeoutMs = null);
}
```

```csharp
public interface IEditableTextControlObject<TScope> : ITextControlObject<TScope>
{
    [UatStep(UatEffectiveStepKeyword.When, "I enter {value} into {control}", CommandId = "Control.Enter")]
    TScope Enter(string? text, int? timeoutMs = null);

    [UatStep(UatEffectiveStepKeyword.When, "I set {control} to {value}", CommandId = "Control.SetText")]
    TScope SetText(string? text, int? timeoutMs = null);
}
```

And the two-phrases-one-method case:

```csharp
public interface IElementObject<TScope>
{
    [UatStep(UatEffectiveStepKeyword.Then, "{control} should be visible",     HasLiteral = true, Literal = true,  CommandId = "Control.AssertVisible")]
    [UatStep(UatEffectiveStepKeyword.Then, "{control} should not be visible", HasLiteral = true, Literal = false, CommandId = "Control.AssertVisible.False")]
    TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
}
```

Three consequences worth stating, because they are the point of the change:

1. **The `{control}` binding is implicit.** The receiver *is* the control and the
   decorated method *is* the target, so nothing has to name `"control"` or
   `"SetText"` as strings in a table.
2. **The engine never references the control type.** Discovery records the method
   *name*; execution invokes it by name on whatever control the lexicon resolved.
   That is what lets one catalog serve six technologies.
3. **An app can add a verb without touching the engine.** A `[UatStep]` on an app
   control type is discovered on the same pass as the Core ones, because
   `ControlStepTypes()` concatenates the Core types with the control property
   types found during page discovery.

## PageObject Method Attributes

PageObject methods can expose page-specific commands.

```csharp
[UatName("Login")]
public class LoginPage : PageObjectBase<LoginPage>
{
    [UatPhrase(UatEffectiveStepKeyword.When, "I sign in with credentials")]
    public Task SignInAsync(UatTable table, CancellationToken cancellationToken = default)
    {
        ...
    }
}
```

Page-specific commands are used for actions that are richer than a single generic
control operation — a multi-field form step being the usual case. A page does
**not** need a phrase to assert it is open: `Then I should be on the {page} page`
is an engine verb and covers every page, so the original `AssertOpenAsync`
example has been dropped from this snippet.

## Custom Command Classes

Projects can define command classes for domain-specific actions.

```csharp
public sealed class CustomerUatCommands
{
    [UatPhrase(UatEffectiveStepKeyword.When, "I create a customer")]
    public Task CreateCustomerAsync(
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        ...
    }
}
```

Custom commands are discovered from configured command assemblies.

## Assembly Discovery

The runner discovers pages, controls, and commands from configured assemblies.

```text
Page assemblies
Control assemblies
Command assemblies
App bootstrap assembly
```

Discovery steps:

1. Load configured assemblies.
2. Find PageObject types.
3. Find public ControlObject properties on PageObjects.
4. Skip members marked `[UatIgnore]`.
5. Apply `[UatName]` when present.
6. Infer names when no `[UatName]` exists and name inference is enabled.
7. Find methods marked `[UatPhrase]` or `[UatAction]`.
8. **Discover control verbs from `[UatStep]`** — see below.
9. Add project-specific command handlers.
10. Validate page names, control names, command phrases, and table requirements.

> **Superseded (2026-09):** step 8 read "Add built-in generic commands for known
> capabilities", which meant "read them off a hand-written table". It is now a
> reflection pass with no table behind it.

### Step 8 in detail

`UatCatalogBuilder.DiscoverBindings` walks the candidate types, takes each
public instance method declared on the type, and reads every `[UatStep]` on it:

```text
for each type in (Brinell.Core step types + discovered app control types)
  for each public instance method declared on that type
    for each [UatStep] on that method
      skip if (keyword, phrase) already seen      <- dedup
      record (keyword, phrase, commandSuffix, methodName, valueArgument, literal)
```

Two things that pass carries:

- **Dedup on `(keyword, phrase)`.** A verb declared on a base interface is reached
  through every interface that inherits it, so the same phrase arrives several
  times; the first wins and the rest are dropped. Without this, discovery would
  trip `UATD003` (duplicate phrase) on its own output.
- **The value argument is inferred from the phrase.** The one placeholder that is
  not `{control}` is the value; a phrase with no such placeholder is a literal
  variant and takes its argument from `Literal` instead. `{control} should be
  visible` therefore calls `AssertVisible(true)` with no capture at all.

## Two Projections, One Source

The catalog exists twice: once with handlers, for execution, and once
phrases-only, for spec/format tests and the Presenter's binding preview. In the
original design those were two hand-written tables, plus a third inside the
Presenter — three copies of the same 18 rows, kept in step by hand.

They are now the *same discovery pass*, called twice with different arguments:

```text
UatCatalogBuilder.DiscoverBindings(types)
        |
        +--> RegisterControlVerbs(catalog, types, "Builtin.", handlerFactory)   runtime
        |      UatReflectionRuntime.CreateCommandCatalog()
        |      handler -> InvokeControlMethodAsync(control, methodName, value, literal)
        |
        +--> RegisterControlVerbs(catalog, CoreStepTypes, "Spec.", null)        spec
               UatSpecCommandCatalog.RegisterDefault()
               no handler: phrases only
```

The Presenter no longer has a table of its own; `UatWorkspaceService` calls
`UatSpecCommandCatalog.CreateDefault()` like everything else.

The property this buys is stronger than "we remembered to update both": the two
projections *cannot* disagree, because neither of them holds the vocabulary. The
only difference between them is the command-id prefix and whether a handler is
attached, which is why `RuntimeAndSpecProjections_AgreeOnControlVerbs` can compare
them on the shared `CommandId` suffix.

The one thing this does **not** catch is a verb that loses its `[UatStep]`
entirely — both projections then drop it together and stay green. That gap is
recorded as an open follow-up in
[05-implementation-review.md](../uat/attribute-catalog/05-implementation-review.md)
Finding 1.

The decision is recorded as
[AD-011](../../.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md),
whose point is to keep a hand-written phrase table from growing back.

## Configuration

Assembly registration should live outside ordinary UAT files.

The runner resolves configuration in this order:

```text
Runner defaults
  -> runner profile
  -> nearest uat.config.md from parent folder search
  -> UAT file metadata for labels and tags
```

The UAT file metadata should describe the test, not the code loading setup.

## `uat.config.md`

The runner looks for `uat.config.md` in the loaded UAT folder. If it is not found, it walks parent folders until a config file is found or the search root is reached.

Example:

This is the shipped shape, from
[testsnew/Brinell.Maui.Uat.Tests/uat.config.md](../../testsnew/Brinell.Maui.Uat.Tests/uat.config.md).
Assembly entries are paths to built `.dll` files, not logical names, and the file
grew three sections the original sketch did not have:

```md
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | Appium |
| AppPath | ../../samples/Brinell.Samples.Maui.App/bin/.../Brinell.Samples.Maui.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | ../Brinell.Maui.UITests/bin/.../Brinell.Maui.UITests.dll |
| Controls | ../../srcnew/Brinell.Maui/bin/.../Brinell.Maui.dll |
| Commands | ../../srcnew/Brinell.Uat/bin/.../Brinell.Uat.dll |

## Discovery

| Field | Value |
| --- | --- |
| RequireExplicitUatAttributes | false |
| AllowNameInference | true |

## Reporting

| Field | Value |
| --- | --- |
| ScreenshotOnFailure | true |
| IncludeRuntimeTrace | true |

## Settings

| Field | Value |
| --- | --- |
| Root | TestSettings |
| DefaultFile | testsettings.json |
| ScenarioConvention | scenarios/{ScenarioId}.json |

## Skip Rules

| Tag | EnvironmentVariable |
| --- | --- |
| hardware | BRINELL_UAT_HARDWARE |
| live-api | BRINELL_UAT_LIVE_API |
```

Eight of these exist, one per UAT test project — MAUI, WPF, WinForms, Blazor,
HTML, Stride, Presenter, and the Todo sample.

## Step Matching

The runner matches steps before execution.

```text
1. Normalize whitespace.
2. Substitute scenario-outline parameters.
3. Select command patterns for the effective keyword.
4. Match exact phrases.
5. Match parameterized phrase patterns.
6. Bind arguments.
7. Bind the step table when present.
8. Resolve page and control names.
9. Validate required capabilities.
10. Create an executable step invocation.
```

Unknown, ambiguous, or invalid steps fail validation before execution begins.

## Step Invocation

Each executable step becomes a `UatStepInvocation`. It shipped leaner than the
sketch: rather than flattening the step's source and keyword onto the record, it
carries the parsed `UatStep` and the matched `UatCommandPattern` and projects the
rest.

```csharp
public sealed record UatStepInvocation(
    string ScenarioName,
    UatStep Step,                                  // text, keyword, effective keyword, source
    UatCommandPattern Command,                     // phrase, command id, handler
    IReadOnlyDictionary<string, string> Arguments, // captured placeholders
    UatTable? Table,
    bool FromBackground)
{
    public string MatchedPattern => Command.Phrase;
    public string CommandId => Command.CommandId;
}
```

Page and control resolution is **not** on the invocation. It happens inside the
handler at execution time, against `context.CurrentPageName` — which is what lets
one discovered binding serve any control the lexicon resolves.

## Execution Context

Handlers receive an execution context. It is deliberately small — the registries
the sketch imagined live on the runtime, not on the context passed per step:

```csharp
public sealed class UatExecutionContext
{
    public string? CurrentPageName { get; set; }
    public IDictionary<string, object?> Items { get; } = ...;
    public IList<string> Diagnostics { get; } = [];
}
```

Handler signature:

```csharp
public delegate Task<UatStepResult> UatCommandHandler(
    UatExecutionContext context,
    UatStepInvocation invocation,
    CancellationToken cancellationToken);
```

## Page Context

The runner tracks the current page.

```text
Given I am on the Login page
  -> resolve "Login" in the page lexicon
  -> navigate, wait for ready
  -> set CurrentPageName = "Login"

When I tap Sign in
  -> resolve "Sign in" on the current page
  -> invoke Click() on LoginPage.SubmitButton

Then I should be on the Dashboard page
  -> assert DashboardPage is ready
  -> set CurrentPageName = "Dashboard"
```

A missing page fails with the available page names listed; a missing control fails
with the available controls on the current page. That is the diagnostics
requirement from [08](08%20uat%20diagnostics%20and%20config%20hardening.md), and
it is why both failures name what *was* found.

Commands that navigate or assert a new page update the current page after success.

## Table Binding

Step tables are passed to handlers as structured data.

```md
When I enter credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
```

The matched handler receives:

```text
Table columns: Field, Value
Rows:
  User name = ada@example.com
  Password = correct-password
```

Handlers declare accepted table shapes where possible.

## Diagnostics

Every executed step records a trace.

```text
Step:          When I tap Sign in
Pattern:       I tap {control}
CommandId:     Builtin.Control.Tap
Handler:       Click (invoked by name on the resolved control)
Page:          LoginPage
Control:       SubmitButton
Started:       10:14:23.102
Completed:     10:14:23.411
Result:        Passed
Screenshot:    optional path
AutomationLog: optional path
```

Failure diagnostics include:

```text
Source file
Line number
Original step text
Matched pattern
Handler
Resolved page
Resolved control
Expected value
Actual value
Screenshot path
Automation tree path
Runtime log path
```

## Validation Rules

Only registered command bindings are executable; raw method-call syntax in UAT
Markdown is rejected. The rules that shipped carry diagnostic codes:

| Code | Rule |
| --- | --- |
| `UATB001` | No command binding matches the step. |
| `UATB002` | The step is ambiguous and matches more than one binding. |
| `UATB003` | The step requires a table and none was given. |
| `UATB004` | The step does not accept a table and one was given. |
| `UATD001` | Duplicate page name. |
| `UATD002` | Duplicate control name within one page scope. |
| `UATD003` | Duplicate command phrase for a keyword. |

Binding ambiguity (`UATB002`) is rarer than it looks, because the catalog ranks
candidates before declaring a tie: exact phrases beat parameterised ones, and
among parameterised phrases the one with more literal non-whitespace characters
wins. That is why `{control} should not be visible` never loses to
`{control} should be visible`. The ranking rules are specified in
[01-grammar.md](../uat/attribute-catalog/01-grammar.md) §2.

Still true, and enforced outside the code table: control commands require matching
capabilities (now by interface implementation), and configuration must identify at
least one page assembly.

## Minimal Resolved Example

UAT:

```md
## Scenario: Valid user can sign in

Given I am on the Login page
When I set User name to "ada@example.com"
And I set Password to "correct-password"
And I tap Sign in
Then I should be on the Dashboard page
```

Resolved plan:

```text
1. Builtin.Page.Open                    (engine-owned)
   page name:   Login
   page object: LoginPage
   effect:      CurrentPageName = "Login"

2. Builtin.Control.SetText              (discovered from IEditableTextControlObject.SetText)
   control name: User name  ->  LoginPage.EmailInput
   invoke:       SetText("ada@example.com")

3. Builtin.Control.SetText              (same binding, different control)
   control name: Password   ->  LoginPage.PasswordInput
   invoke:       SetText("correct-password")

4. Builtin.Control.Tap                  (discovered from IClickableControlObject.Click)
   control name: Sign in    ->  LoginPage.SubmitButton
   invoke:       Click()

5. Builtin.Page.AssertOpen              (engine-owned)
   page name:   Dashboard
   effect:      CurrentPageName = "Dashboard"
```

> **Superseded (2026-09):** the original example used a table-driven
> `When I enter credentials` step bound to an `InputCommands.EnterFormTable`
> handler. No such built-in shipped. Two `I set … to …` steps cover it, and a
> genuine multi-field form step is a custom `[UatPhrase]` method that reads
> `invocation.Table` — see §Table Binding above.

The runner knows which code to run because discovery builds a finite command
catalog, and every parsed UAT step must match one catalog entry before execution.
What changed since this document was written is only *where the catalog comes
from*: it is projected from `[UatStep]` attributes rather than typed into a table,
so the vocabulary and the code that implements it cannot drift apart.
