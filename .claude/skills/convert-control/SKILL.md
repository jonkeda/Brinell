---
name: convert-control
description: Convert a Brinell control object (e.g. ImageButton.cs, Entry.cs, Label.cs) to the generatable .tpl.cs / .gen.cs format used under Controls/Base. Use when asked to "convert", "migrate", or "update to the new generatable format" a MAUI control object, or to make a control work with Brinell.Generator.
---

# Convert a control object to the generatable format

Turn a hand-written control (`Foo.cs`) into a template (`Foo.tpl.cs`) that holds only
constructors and `*Core` methods, plus a generated partial (`Foo.gen.cs`) holding the
public API.

## The model

The generator reads `*Core` methods and emits public wrappers into a partial class.
The template keeps the "how"; the generated file gets the "what".

| Template declares | Generator emits |
|---|---|
| `protected virtual void ClickCore(IMauiElement e, int? timeoutMs = null)` | `Click(int? timeoutMs = null)` |
| `protected virtual bool? IsFocusedCore(IMauiElement? e)` | `IsFocused()`, `WaitFocused(...)`, `AssertFocused(...)` |
| `protected virtual string? GetTextCore(IMauiElement e)` | `GetText(...)`, `WaitText(...)`, `AssertText(...)` |
| `protected virtual void SetTextCore(IMauiElement e, string text, int? timeoutMs = null)` | `SetText(string text, int? timeoutMs = null)` |

Three generators are registered (`ControlObjectGenerator.CreateDefault()`), in order:
`IsWaitAssertGenerator`, `SetGenerator`, then `ActionGenerator`. First match wins.

**Extra comparison variants.** A `Get*Core` emits only equality by default. Add
`[GenerateComparisons(...)]` (from `Brinell.Core.Interfaces`) for more:

```csharp
[GenerateComparisons(Comparison.Equals | Comparison.Contains | Comparison.Empty)]
protected virtual string? GetTextCore(IMauiElement element) => element.Text;
// → AssertText, AssertTextContains, AssertTextEmpty (+ matching Wait* members)
```

Variants: `Equals`, `Contains`, `StartsWith`, `EndsWith`, `Empty`.

## Matching rules — get these exactly right

A `*Core` method is picked up **only** when all hold:

1. Name ends in `Core`.
2. Modifiers are **`protected virtual`**. Non-virtual, private, or public Core methods
   are **silently skipped — no warning, no error**. This is the most common mistake.
3. First parameter is the platform element (type name contains `Element`).

**When a Core method doesn't match, fix the method — not the generator.** Make it
`protected virtual` and give it an element first parameter if it needs element state.
The contract is fixed; bring the source to it. `Entry.GetPlaceholderCore` and
`Entry.IsReadOnlyCore` are `protected` without `virtual` today and need this.

Which generator claims it:

- **`Is*Core` returning `bool?`/`bool`** → Is/Wait/Assert trio.
- **`Get*Core` returning non-void** → Get/Wait/Assert trio. Extra parameters after the
  element are copied onto every generated signature.
- **anything else `*Core`** → single action wrapper via `RunDoWithElement`.

Two behaviours that drive the conversion:

- **`timeoutMs` is only forwarded when the Core method declares it.** Add
  `int? timeoutMs = null` to a Core method or the generated wrapper takes no timeout,
  which usually breaks the interface signature.
- **No guard is injected.** Despite the "clickable guard" wording in `ActionGenerator`'s
  doc comment, emitted bodies are exactly
  `RunDoWithElement(element => { XCore(element); })`. Any `EnsureClickableCore` /
  `EnsureEnabledCore` call that lived in a hand-written wrapper must move **inside**
  the Core method body.

## Steps

1. **Read the control and its base class.** Know which members are inherited before
   deciding what to convert. Base templates live in
   `srcnew/Brinell.Maui/Controls/Base/`.

2. **Rename to `.tpl.cs`** (`git mv` to keep history) and make the class `partial`.

3. **Reparent to the Base hierarchy** if it still points at the old
   `Brinell.Maui.Controls.ControlBase`: use `Base.ViewBase<TScope>`,
   `Base.FocusableControlBase<TScope>`, or `Base.ClickableControlBase<TScope>`.
   `ViewBase` constructors are `protected`, so constructors in the template must be
   `protected` too (not `public`) when deriving from it.

4. **Sweep every existing `*Core` method against the contract.** Before converting
   anything, make each Core method that should generate `protected virtual` with the
   element as first parameter. Methods that miss the contract are dropped silently, so
   this is the step that prevents losing API without noticing.

   ```csharp
   // before — skipped, no warning
   protected string? GetPlaceholderCore(IMauiElement? element)

   // after — generates GetPlaceholder / WaitPlaceholder / AssertPlaceholder
   protected virtual string? GetPlaceholderCore(IMauiElement? element)
   ```

   Where a Core method needs element state but takes no element, add the parameter and
   read from it instead of re-finding the element.

5. **For each public method, pick one:**
   - Backed by a Core method → delete the public method, make the Core
     `protected virtual`, add `int? timeoutMs = null` if the public signature had it.
   - Hand-written with no Core → extract the body into a new `protected virtual *Core`
     following the naming rules above.
   - Not expressible (see Limits) → keep it hand-written in the template.

6. **Fold guard calls into the Core bodies** (see above).

7. **Guards use the `Ensure*` prefix.** `ActionGenerator` skips any method whose name
   starts with `Ensure`, so guards stay `protected virtual` (overridable) and generate
   no public wrapper. Name every new guard `Ensure*`.

8. **Generate and build:**
   ```
   tools/Scripts/CreateMaui.Bat
   dotnet build srcnew/Brinell.Maui/Brinell.Maui.csproj
   ```
   `CreateMaui.Bat` rebuilds the generator, then processes every `.tpl.cs` under
   `Controls/Base` recursively. For a control elsewhere, run the CLI directly:
   ```
   tools/Brinell.Generator.Cli/bin/Release/net10.0/Brinell.Generator.Cli.exe --input <folder-or-file>
   ```
   `--input` takes a `;`-separated list of files and/or folders; output is always
   `<name>.gen.cs` beside the input.

9. **Diff the public surface against the original.** The generated API must match what
   the old class exposed. Extra members (step 7) and members dropped for missing the
   contract (step 4) both show up here. Then build the full solution — controls are
   widely referenced.

## Limits — do not try to generate these

- **Multiple `Get*Core` overloads** differing only by parameters — they collide on the
  same generated name. The generator now fails with a clear error instead of emitting
  uncompilable code; rename one of the Core methods.
- **Bespoke assertion logic.** Generated `Assert*` synthesizes a message naming the
  property and locator, and the caller's `message` still wins — but if an assert needs
  comparison logic beyond the `[GenerateComparisons]` variants, keep it hand-written.

When you hit a limit, leave the member hand-written and say so — do not silently drop it
or change its behaviour.

## Verifying

Compare generated output against the original public API. A quick check:

```
grep -E "public (TScope|bool|string|int)" Foo.gen.cs
```

Every member the old class exposed should appear, and nothing extra. Then:

```
dotnet build Brinell.sln
```

Report honestly which members were generated, which stayed hand-written, and any public
API that changed shape.

## Worked example

`ClickableControlBase` (`Controls/ClickableControlBase.cs` → `Controls/Base/ClickableControlBase.tpl.cs`):

- Deleted six hand-written wrappers (`Click`, `DoubleClick`, `RightClick`, `Hover`,
  `LongPress`, `Press`) — all regenerated from their Core methods.
- Moved `EnsureClickableCore(element)` from each wrapper into each Core body.
- `PressCore` went `private` → `protected virtual` so the generator could see it.
- `HoverCore` / `LongPressCore` gained `int? timeoutMs = null` to match interface signatures.
- `IsClickableCore` gained `virtual` so the Is/Wait/Assert trio would generate.
- `EnsureEnabledCore` / `EnsureClickableCore` were made non-virtual to suppress bogus
  public actions — revert to `protected virtual` once the `Ensure` prefix rule lands.
- `AssertClickable` lost its custom failure message — an accepted limit, flagged to the user.
