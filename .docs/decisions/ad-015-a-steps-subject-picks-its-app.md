# AD-015: A Step's Subject Picks Its App

A scenario with several apps names who does each step: `When Daan taps Save`. The name is the
step's **subject**, and it is an app or a user from the `Apps` table (or the file that
`AppsFile` names). A step with a subject binds only to a phrase **declared** with `{user}` (or
`{app}`) as its first parameter. A phrase that can be said of a named user has that form as a
second keyword attribute on the same method:

```csharp
[UatWhen("I tap {control}", CommandId = "Control.Tap")]
[UatWhen("{user} taps {control}", CommandId = "Control.Tap")]
void Click(int? timeoutMs = null);
```

The engine infers no verb forms. `Brinell.Core` declares the named form of every built-in `I ...`
control verb, and the runtime declares the named forms of its page steps (`{user} is on the
{page} page`, `{user} should be on the {page} page`, `{user} should see {text}`).

- **The subject sticks.** The app it picks stays current, as it does after `{app} uses the app`,
  so a later `I` step runs on it. The result JSON records the app of every step.
- **A user picks an app.** An app name picks that app. A user with one app picks it. A user with
  several apps picks the one used last in the scenario, and before that the step fails at bind
  time, asking for the app by name. `TestAppSet.Resolve` in `Brinell.Core.Testing` holds the same
  rules for UI tests.
- **`{user}` is the acting user.** It receives the subject, or the current app's user when the
  step says `I`, and it is never a control verb's value. In first place it switches apps;
  elsewhere it is only checked. It is checked against the `Apps` table at bind time, and a suite
  without that table cannot name a user.
- **Each app binds against its own head's catalog.** `UatCatalogSet.CatalogFor(app)` is the one
  place that decides what a step can see, so a MAUI page and a web page of the same name never
  meet in one lookup (AD-014).

**Why:** a scenario about two people reads as what each of them does. Declaring the named form
keeps what binds visible in the phrase list (the Presenter, the discovery report, the catalog in
the result JSON), lets the author choose which phrases a user can say, needs no table of verb
forms, and works for a vocabulary in another language. A bind-time check keeps a misspelled name
or a step no app can run from starting any app. Each phrase text is still declared once (AD-011);
one method carries two phrases.

**Ruled out:** inferring the first-person phrase from a third-person step (it guessed verb forms,
could bind a step the author did not mean, and only knew English); treating any capitalised first
word as a subject (`Todos should have ...` starts with a control); a subject for one step only (an
`I` step could then not be read without scanning back through the scenario); bracket syntax such
as `[Daan] tap OK`.

**Broken when:** a step with a subject binds to a phrase whose first parameter is not `{user}` or
`{app}`; a subject is found that is not in the `Apps` table; a name in the table is also a page or
control name without `UATB005`; a built-in `I ...` phrase has no `{user}` form with the same
command; `{user}` reaches a control method as its value; a step runs on another app than the one
bound for it.
