# Command Extensibility And Discovery

This document discusses how the UAT runner can start with a small command set while staying extensible.

The main idea is that many UAT commands should come from Brinell object capabilities. A button can be tapped. An input field can receive text. A checkbox can be checked. A page can be asserted visible. The runner should use those capabilities without forcing every project to hand-write every basic command.

## Starting Assumption

The first runner only needs a limited set of commands.

Starter commands:

| User wording | Bound capability |
| --- | --- |
| `I am on the {page} page` | Page object visible/open assertion |
| `I tap {control}` | Button or tappable ControlObject |
| `I enter {value} into {control}` | Input ControlObject |
| `I select {value} in {control}` | Picker/dropdown ControlObject |
| `I check {control}` | Checkbox/toggle ControlObject |
| `I uncheck {control}` | Checkbox/toggle ControlObject |
| `I should see {text}` | Text visibility assertion |
| `I should see the {page} page` | Page object visible/open assertion |
| `I should see {value} in {control}` | Control value/text assertion |

This should cover many first UATs without needing a large language.

## Extensibility Layers

There are three useful layers:

1. Built-in generic commands.
2. Commands derived from ControlObject and PageObject capabilities.
3. Project-specific custom commands.

```text
Built-in command catalog
  + discovered PageObject bindings
  + discovered ControlObject bindings
  + project custom commands
  = executable UAT command catalog
```

## ControlObject Capabilities

Control objects can expose capabilities such as:

```text
Tap
EnterText
Select
Check
Uncheck
AssertVisible
AssertText
AssertValue
```

The runner can map capability interfaces to UAT commands.

Example:

```text
Control implements ITappableControl
  -> supports "I tap {control}"

Control implements ITextInputControl
  -> supports "I enter {value} into {control}"

Control implements ISelectableControl
  -> supports "I select {value} in {control}"
```

This keeps the first command set small while allowing many actual controls to participate.

## Attributes On ControlObject Methods

Yes, attributes on ControlObject methods are a good option.

Example:

```csharp
public sealed class ButtonControl : ControlObject
{
    [UatAction("tap")]
    [UatPhrase("I tap {control}")]
    public Task TapAsync(CancellationToken cancellationToken = default)
    {
        ...
    }
}
```

For an input:

```csharp
public sealed class TextInputControl : ControlObject
{
    [UatAction("enter")]
    [UatPhrase("I enter {value} into {control}")]
    public Task EnterTextAsync(string value, CancellationToken cancellationToken = default)
    {
        ...
    }
}
```

The runner can discover these methods and expose them only when the resolved control has the required action.

This avoids hard-coding every control type in the runner.

## Attributes On PageObject Methods

Page objects can also expose UAT actions and assertions.

Example:

```csharp
public sealed class LoginPage : PageObject
{
    [UatPageName("Login")]
    public static string UatName => "Login";

    [UatAction("sign in")]
    [UatPhrase("I sign in with credentials")]
    public Task SignInAsync(UatTable table, CancellationToken cancellationToken = default)
    {
        ...
    }

    [UatAssertion("open")]
    [UatPhrase("I should see the Login page")]
    public Task AssertOpenAsync(CancellationToken cancellationToken = default)
    {
        ...
    }
}
```

This is useful for higher-level actions that are page-specific and too rich for generic control commands.

## Naming Rules

The runner can derive names from PageObject and ControlObject member names.

Examples:

```text
LoginPage              -> Login
CustomerDetailsPage    -> Customer Details
SignInButton           -> Sign In
UserNameInput          -> User Name
PasswordField          -> Password
RememberMeCheckbox     -> Remember Me
CountryPicker          -> Country
```

Suggested suffix stripping:

```text
Page
Button
Input
Field
TextBox
Checkbox
Toggle
Picker
Dropdown
List
Grid
Label
Text
```

Suggested word splitting:

```text
PascalCase -> Pascal Case
camelCase  -> Camel Case
snake_case -> Snake Case
kebab-case -> Kebab Case
```

## Name Override Attributes

Naming rules should be defaults only. Attributes should override names.

Example:

```csharp
public sealed class LoginPage : PageObject
{
    [UatName("Sign in")]
    public ButtonControl SubmitButton { get; }

    [UatName("User name")]
    public TextInputControl EmailInput { get; }
}
```

The first version should expose one canonical UAT name per page or control.

Example:

```text
I tap Sign in -> SubmitButton
```

Do not support aliases in v1. If a control needs a different authoring name, use `[UatName]` to choose the single supported name.

## Attribute Sketch

Possible attributes:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UatNameAttribute : Attribute
{
    public UatNameAttribute(string name) { ... }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatPhraseAttribute : Attribute
{
    public UatPhraseAttribute(string phrase) { ... }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatActionAttribute : Attribute
{
    public UatActionAttribute(string actionName) { ... }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UatIgnoreAttribute : Attribute
{
}
```

`UatIgnore` is important so helper controls and internal actions do not leak into the command catalog.

## Assembly Discovery

The runner needs to know which assemblies contain PageObjects, ControlObjects, and custom commands.

Inputs:

```text
Page assemblies
Control assemblies
Command assemblies
Optional app bootstrap assembly
```

Discovery flow:

```text
Load configured assemblies
Find PageObject types
Find ControlObject types
Find methods with UAT attributes
Find custom command classes
Build page registry
Build control registry
Build command catalog
Validate duplicate names and ambiguous phrases
```

## Where To Configure Assemblies

There are three reasonable places.

### Option 1: Inside Each UAT File

```md
## Metadata

| Field | Value |
| --- | --- |
| Target | MAUI |
| PageAssemblies | Example.App.UITests.Pages |
| CommandAssemblies | Example.App.UITests.UatCommands |
```

Advantages:

- One file can be self-contained.
- Easy for experiments.

Tradeoffs:

- Repeats configuration in every file.
- Easy for files in the same suite to drift.
- Assembly paths are not really scenario content.

### Option 2: Separate Markdown Config In Folder Or Parent Folder

Example file:

```text
uat.config.md
```

Example contents:

```md
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Adapter | FlaUI |
| AppBootstrap | Example.App.UITests.MauiAppBootstrap |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Example.App.UITests.Pages |
| Controls | Brinell.Maui.Controls |
| Commands | Example.App.UITests.UatCommands |
```

Discovery:

```text
Start at UAT file folder
Look for uat.config.md
If not found, walk parent folders
Merge config with runner defaults
Let UAT file metadata override only safe fields
```

Advantages:

- Good for folders full of UAT files.
- Keeps assembly/runtime setup out of test content.
- Easy to inherit from parent folders.
- Still human-readable.

Tradeoffs:

- Requires config lookup rules.
- Runner UI must show which config file was used.

### Option 3: Runner Profile

The MAUI runner app can define profiles:

```text
Profile: Example App - Local
Target: MAUI
Adapter: FlaUI
App bootstrap: Example.App.UITests.MauiAppBootstrap
Assemblies:
  Example.App.UITests.Pages
  Example.App.UITests.UatCommands
```

Advantages:

- Best for daily local use.
- Avoids config in UAT files.
- Can include local machine paths and launch settings.

Tradeoffs:

- UAT folder is less portable unless profile is exported.
- CI needs a matching profile or command-line config.

## Recommended Config Model

Use a layered config model.

```text
Runner defaults
  -> runner profile
  -> nearest uat.config.md from parent folder search
  -> UAT file metadata
```

Recommended first version:

```text
1. Runner profile for local app launch and adapter selection.
2. uat.config.md for page and command assemblies.
3. UAT file metadata for tags, area, and suite-level labels.
```

Keep assembly registration out of ordinary UAT files unless there is a special reason.

## Proposed `uat.config.md`

```md
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Adapter | FlaUI |
| AppBootstrap | Example.App.UITests.MauiAppBootstrap |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Example.App.UITests.Pages |
| Controls | Example.App.UITests.Controls |
| Commands | Example.App.UITests.UatCommands |

## Discovery

| Field | Value |
| --- | --- |
| IncludeInheritedConfigs | true |
| RequireExplicitUatAttributes | true |
| AllowNameInference | true |
```

## Discovery Rules

Suggested first rules:

1. Load assemblies from runner profile and `uat.config.md`.
2. Discover PageObject types.
3. Discover public ControlObject properties on PageObjects.
4. Skip members marked `[UatIgnore]`.
5. Use `[UatName]` when present.
6. Otherwise infer names from type/member names if `AllowNameInference` is true.
7. Discover methods marked `[UatPhrase]` or `[UatAction]`.
8. Add built-in generic commands for known control capabilities.
9. Validate that every exposed name is unique within its scope.
10. Validate that every exposed phrase is unambiguous.

## Page And Control Scope

Names should be scoped.

Page names are global:

```text
Login -> LoginPage
Customers -> CustomersPage
```

Control names are page-scoped:

```text
Login.Sign in -> LoginPage.SubmitButton
Customers.Search -> CustomersPage.SearchInput
```

The current page context resolves short control names:

```md
When I tap Search
```

If there is no current page, or the name is ambiguous, validation fails.

Explicit page scoping can be added later:

```md
When I tap Search on the Customers page
```

## Open Design Questions

- Should generic actions be discovered from interfaces such as `ITappableControl`, or from method attributes such as `[UatAction("tap")]`?
- Should `[UatName]` live in Brinell core attributes, or in a separate UAT package?
- Should name inference be enabled by default, or should v1 require explicit attributes for all exposed controls?
- Should assembly loading accept assembly names only, or also file paths?
- Should `uat.config.md` be the only portable config format, with runner profiles used only as local overrides?

## Recommended First Slice

1. Define a small built-in command catalog.
2. Add UAT attributes for names, phrases, actions, and ignore.
3. Add page assembly registration through `uat.config.md`.
4. Discover PageObject types and ControlObject properties.
5. Infer names from member names, with `[UatName]` overrides.
6. Bind generic commands only to controls with matching capabilities.
7. Report the resolved page/control/method for every step.

This gives the runner a small, understandable command set while allowing projects to grow their UAT vocabulary through attributes and assembly registration.
