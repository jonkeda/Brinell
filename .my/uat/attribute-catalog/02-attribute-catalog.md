# 02 — The attribute-driven catalog

This is the core of the proposal: delete the two hand-written phrase tables and
**discover** them from attributes on the control methods and page members.

Recall the two tables today:

- `UatReflectionRuntime.CreateCommandCatalog()` — 18 `catalog.Register(...)`
  calls, each phrase wired to an `InvokeControlMethodAsync(..., "SetText",
  "value")`-style handler.
- `UatSpecCommandCatalog.RegisterDefault()` — the same 18 phrases, wired to
  string command ids for binding-only format tests.

Both encode the *same* facts — "the phrase `I set {control} to {value}` is a
`When` that calls `SetText(value)` on a control" — in a place that has to know
about every control method. That fact belongs **on `SetText`**.

---

## 1. The attribute schema

Reuse the existing `[UatPhrase]` shape and add one method-targeting attribute for
the *built-in control verbs*, plus a small marker for literal-bound variants.

```csharp
/// <summary>
/// Declares that this control-object method backs a UAT step phrase.
/// The receiver control fills the implicit {control} placeholder; method
/// parameters fill the named placeholders.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatStepAttribute : Attribute
{
    public UatStepAttribute(UatEffectiveStepKeyword keyword, string phrase) { ... }

    public UatEffectiveStepKeyword Keyword { get; }
    public string Phrase { get; }

    /// <summary>
    /// For boolean-verb variants (visible / not visible). When set, the method's
    /// single bool parameter is supplied from here instead of from a placeholder.
    /// </summary>
    public bool? Literal { get; init; }

    /// <summary>Optional stable id; defaults to DeclaringType.Method.</summary>
    public string? CommandId { get; init; }
}
```

Why a new attribute rather than overloading `[UatPhrase]`:

- `[UatPhrase]` today sits on **root / phrase-class** methods where the *whole*
  step is the phrase and there is no implicit receiver.
- `[UatStep]` sits on **control methods** where `{control}` is implicit (the
  receiver) and the remaining placeholders map to parameters. Keeping them
  distinct keeps the discovery rules unambiguous. (If preferred, this can be a
  second constructor on `[UatPhrase]` guarded by an `ImplicitControl` flag —
  a naming decision, not a design one.)

### Declaring the verbs where they live

The 18 built-ins move onto the control base types in `Brinell.Maui` (and the
shared control contracts). Illustrative:

```csharp
public abstract class ControlObject<TPage>
{
    [UatStep(UatEffectiveStepKeyword.When, "I tap {control}")]
    public void Click() { ... }

    [UatStep(UatEffectiveStepKeyword.Then, "{control} should be visible", Literal = true)]
    [UatStep(UatEffectiveStepKeyword.Then, "{control} should not be visible", Literal = false)]
    public void AssertVisible(bool expected) { ... }
}

public class Entry<TPage> : ControlObject<TPage>
{
    [UatStep(UatEffectiveStepKeyword.When, "I enter {value} into {control}")]
    public void Enter(string value) { ... }

    [UatStep(UatEffectiveStepKeyword.When, "I set {control} to {value}")]
    public void SetText(string value) { ... }

    [UatStep(UatEffectiveStepKeyword.When, "I clear {control}")]
    public void Clear() { ... }

    [UatStep(UatEffectiveStepKeyword.Then, "{control} should contain {value}")]
    public void AssertTextContains(string value) { ... }
}
```

The complete mapping from the current hardcoded rows to their new homes:

| Phrase | Keyword | Method | Literal |
| --- | --- | --- | --- |
| `I tap {control}` | When | `Click` | — |
| `I enter {value} into {control}` | When | `Enter(value)` | — |
| `I set {control} to {value}` | When | `SetText(value)` | — |
| `I clear {control}` | When | `Clear` | — |
| `I check {control}` | When | `Check` | — |
| `I uncheck {control}` | When | `Uncheck` | — |
| `I select {value} from {control}` | When | `SelectByText(value)` | — |
| `{control} should contain {value}` | Then | `AssertTextContains(value)` | — |
| `{control} should equal {value}` | Then | `AssertText(value)` | — |
| `{control} should be visible` | Then | `AssertVisible` | `true` |
| `{control} should not be visible` | Then | `AssertVisible` | `false` |
| `{control} should be enabled` | Then | `AssertEnabled` | `true` |
| `{control} should be checked` | Then | `AssertChecked` | `true` |
| `{control} should be unchecked` | Then | `AssertChecked` | `false` |
| `{control} should have selected {value}` | Then | `AssertSelectedText(value)` | — |

The two page-scoped verbs (`I am on the {page} page`, `I should be on the {page}
page`, `I should see {text}`) are **not** control methods — they stay as runtime
handlers registered by the catalog builder, because their target is the page
graph, not a single member. They are the only rows that remain central, and
there are three of them.

---

## 2. Discovery algorithm

A new `UatCatalogBuilder` produces the catalog from types instead of literals.
It runs during `CreateCommandCatalog()` in place of the 18 `Register` calls.

```
build(catalog, controlTypes, pageTypes, roots, phraseClasses):
    # (a) page-graph verbs — the only central rows
    register "I am on the {page} page"        (Given → OpenPage)
    register "I should be on the {page} page" (Then  → AssertPage)
    register "I should see {text}"            (Then  → AssertAnyControlText)

    # (b) built-in control verbs — discovered
    for type in controlTypes:                 # Entry<>, Picker<>, ControlObject<>, ...
        for method in type.PublicInstanceMethods:
            for attr in method.GetCustomAttributes<UatStepAttribute>():
                register attr.Keyword, attr.Phrase,
                         id  = attr.CommandId ?? $"{type}.{method}",
                         handler = InvokeControlMethod(method, attr.Literal, valueParams)

    # (c) custom phrases — unchanged
    registerRootPhrases(roots)                # [UatPhrase] on the fixture root
    registerPhraseClassPhrases(phraseClasses) # [UatPhrase]/[UatPhraseClass]
```

Parameter binding for a discovered control verb generalises the current
hardcoded `InvokeControlMethodAsync(..., "control", method, valueParam)`:

- The **receiver** is the control resolved from the `{control}` capture against
  the current page's lexicon.
- Each remaining method parameter is filled by the phrase capture whose
  placeholder name equals the parameter name (`value` → `value`), converted with
  the existing `ConvertArgument` rules (string/int/double/bool/enum).
- If `Literal` is set, the method's single `bool` parameter is filled from
  `Literal` and no placeholder is expected.

Control types are already enumerated during page discovery
(`DiscoverControls`), so the set of `controlTypes` is the distinct property
types found across discovered pages plus their base types — no new registration
surface for app authors.

### De-duplication

Because a phrase can be declared on a base type (`ControlObject.Click`) and
inherited by many controls, discovery must register each `(keyword, phrase)`
pair **once**. The catalog already guards this for root phrases
(`catalog.Patterns.Any(... phrase.Equals ...)`); the builder applies the same
guard so `I tap {control}` is registered a single time even though every control
inherits `Click`.

---

## 3. Building both catalogs from one source

The spec/binding-only catalog (`UatSpecCommandCatalog`) needs the same phrases
without handlers — it exists so format tests can check that a `.uat.md` binds
without launching an app. Project it from the **same attributes**:

```csharp
public static void RegisterDefault(UatCommandCatalog catalog)
{
    UatCatalogBuilder.RegisterPhrasesOnly(
        catalog,
        controlTypes: UatControlContracts.Default, // the same base control types
        includePageVerbs: true);
}
```

`RegisterPhrasesOnly` walks the identical attribute set but passes
`handler: null` and derives the command id from `[UatStep].CommandId ??
Type.Method`. The result: the two tables can never drift, because there is only
one table — the attributes — and two projections of it.

To keep the *format tests* honest during migration, add a test that asserts the
two projections register the **same set of `(keyword, phrase)` pairs**. That
test is what lets us delete the hand-written `RegisterDefault` body with
confidence.

---

## 4. What the author sees — nothing new

- Scenario files are byte-for-byte unchanged.
- Control names still come from `[UatName]` / inference (layer 3 in
  [01-grammar.md](01-grammar.md)).
- A team adding a new control verb now writes it **once**, next to the method:

  ```csharp
  [UatStep(UatEffectiveStepKeyword.When, "I long-press {control}")]
  public void LongPress() { ... }
  ```

  and it is instantly available in every scenario and in the spec catalog — no
  edit to `CreateCommandCatalog`, no edit to `RegisterDefault`, no invented
  command id.

This is the payoff: the phrase, its keyword, and its target are one declaration
at the point of definition, and the two central tables disappear.
