# AD-015: A Step's Subject Picks Its App

A scenario with several apps names who does each step: `When Daan taps Save`. The name is the
step's **subject**, and it is an app or a user from the `Apps` table (or the file that
`AppsFile` names). The engine removes the subject, turns the verb back into the first person, and
binds the result to the ordinary `I ...` phrase on that app: `Daan taps Save` is `I tap Save` on
Daan's app. The rewrite tries candidates (the word as written, `is`/`has`/`does`/`goes`, and the
word without `s`, `es`, or with `ies` made `y`), and it binds only when exactly one first-person
phrase matches.

- **The subject sticks.** The app it picks stays current, as it does after `{app} uses the app`,
  so a later `I` step runs on it. The result JSON records the app of every step.
- **A user picks an app.** An app name picks that app. A user with one app picks it. A user with
  several apps picks the one used last in the scenario, and before that the step fails at bind
  time, asking for the app by name. `TestAppSet.Resolve` in `Brinell.Core.Testing` holds the same
  rules for UI tests.
- **`{user}` is reserved.** A phrase parameter named `{user}` receives the acting user: the
  subject, or the current app's user when the step says `I`. In first place it also switches
  apps. It is checked against the table at bind time.
- **Each app binds against its own head's catalog.** `UatCatalogSet.CatalogFor(app)` is the one
  place that decides what a step can see, so a MAUI page and a web page of the same name never
  meet in one lookup (AD-014).

**Why:** a scenario about two people reads as what each of them does. The engine can find the
actor without a second phrase per verb, because the actor always takes the place of `I`. Binding
the subject when the file is bound makes a misspelled name or a step no app can run fail before
any app starts.

**Ruled out:** a second attribute or phrase per verb for the third person (it breaks AD-011's
"declared once"); treating any capitalised first word as a subject (`Todos should have ...` starts
with a control); a subject for one step only (an `I` step could then not be read without scanning
back through the scenario); bracket syntax such as `[Daan] tap OK`.

**Broken when:** a step with a subject binds to a phrase that does not start with `I` or
`{user}`; a subject is found that is not in the `Apps` table; a name in the table is also a page
or control name without `UATB005`; a step runs on another app than the one bound for it.
