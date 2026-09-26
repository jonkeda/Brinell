# AD-014: One Scenario File Serves Every Head

An app with more than one head (the Todo sample's MAUI app and web app) has one set of `.uat.md`
scenario files. Each head has its own UAT suite, which runs those files - the web suite links
them from the MAUI one - through page objects and phrase classes of its own that use the same
names and phrases. `Head` in a suite's `uat.config.md` names its head; a scenario's head tags
(`@maui`, `@web`) say where it runs, and the engine skips it on any other head. A scenario with no
head tag runs on every head.

Within one scenario, several apps (`{app} uses the app`, an `Apps` table, `IUatSessionHost`) are
apps of the suite's head: they share its page and phrase types.

**Why:** a journey is the same on both heads; two copies of its scenario text drift, and a
difference between heads should show as a failing scenario, not as two files that say different
things. Evidence: `.my/html/review-html-vs-maui.md` section 4 (items 7, 8),
`.my/html/todo-blazor.md` section 4.

**Ruled out:** a copy of a scenario file per head; head-specific wording for the same step; a
skip rule or environment variable standing in for a head tag; apps of different heads in one
scenario, until the engine has a page catalog per app.

**Broken when:** a scenario file exists in two suites' folders; a step passes on one head only
because the other head's phrase class words it differently; a suite with `Head` runs a scenario
tagged only for another head; a session switch resolves page types from another head.
