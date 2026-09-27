# AD-014: One Scenario File Serves Every Head

An app with more than one head (the Todo sample's MAUI app and web app) has one set of `.uat.md`
scenario files. Each head has its own UAT suite, which runs those files - the web suite links
them from the MAUI one - through page objects and phrase classes of its own that use the same
names and phrases. `Head` in a suite's `uat.config.md` names its head; a scenario's head tags
(`@maui`, `@web`) say where it runs, and the engine skips it on any other head. A scenario with no
head tag runs on every head.

Within one scenario, several apps (an `Apps` table, `IUatSessionHost`, a step's subject per
AD-015) are apps of the suite's head and share its page and phrase types. The exception is a
suite whose `Head` is `mixed`: it runs only scenarios tagged `@mixed`, its apps name their own
`Head`, and each app binds against that head's catalog (`IUatSessionHost.CompositionFor`,
`UatCatalogSet`). Mixed scenarios live in the mixed suite, not beside the one-head files.

**Why:** a journey is the same on both heads; two copies of its scenario text drift, and a
difference between heads should show as a failing scenario, not as two files that say different
things. Evidence (at 7162ac7): [review-html-vs-maui.md](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/html/review-html-vs-maui.md) section 4
(items 7, 8), [todo-blazor.md](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/html/todo-blazor.md) section 4.

**Ruled out:** a copy of a scenario file per head; head-specific wording for the same step; a
skip rule or environment variable standing in for a head tag; apps of different heads in one
scenario outside a `mixed` suite.

**Broken when:** a scenario file exists in two suites' folders; a step passes on one head only
because the other head's phrase class words it differently; a suite with `Head` runs a scenario
tagged only for another head; a session switch resolves page types from another head; a one-head
suite runs a `@mixed` scenario.
