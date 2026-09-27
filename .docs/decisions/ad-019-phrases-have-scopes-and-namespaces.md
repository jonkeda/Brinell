# AD-019: Phrases Have Scopes and Namespaces

A step binds only to a phrase it can see from where it is written. That is the union of:

1. **the current page**: its control verbs and page phrases, followed through the steps before
   anything runs (`I am on`, a returned page object, `[UatNavigatesTo]`, a callable's steps);
2. **the file**: its own callables;
3. **its folder and every folder above it** under `Scenarios/`. A folder is a namespace
   (`Invoicing/Credit notes`): phrase files in it, and C# phrase classes and pages declared in it
   with `[UatPhraseClass(Namespace = ...)]` or `[UatNamespace]` (class or assembly);
4. **the namespaces it uses**: the file's `Using` metadata row, and the config's `## Using`;
5. **global**: built-ins, generic control verbs, the fixture's phrases and phrase classes without a
   namespace.

- **No shadowing.** A step that matches phrases in two visible scopes is ambiguous (`UATB002`),
  never "innermost wins". A page name seen in two namespaces is ambiguous too; the step names it
  with its namespace (`Accounts/Detail`).
- **A folder is a namespace**, named by its path, compared ignoring case. Pages belong to the
  namespace of the phrases that drive them; one name in two namespaces is two pages.
- **`Using` is explicit and not transitive.** A file sees what it names, one row per file. A
  `Using` that names nothing is an error (`UATB011`); one no step needs is a warning (`UATB012`).
- **Callables bind where they are written.** A callable's steps use the phrases seen from its
  file or folder and the config's usings, not the caller's; they run in the caller's app and page.
- **A phrase of another scope is named, not hidden.** A step that matches only phrases it cannot
  see is `UATB010`, saying where they are.
- **No qualified steps** (`When [Accounts] I archive it`). Two used namespaces with one phrase:
  stop using one, or rename a phrase.

**Why:** one flat catalog fails at the size of an app with many modules and teams: two modules
cannot both say "I add a line", a tester cannot find the phrase that fits here in hundreds, and a
team cannot change its phrases without checking everyone's scenarios. Scopes keep collisions local
to the files that use both namespaces, and every step's meaning readable from its file: its folder,
its `Using` row, its page.

**Ruled out:** innermost-scope-wins (one sentence would mean two things in one place); making every
namespace visible everywhere (the flat list again); transitive usings (a file's vocabulary would
change when another file changes); CLR namespaces as phrase namespaces (they change when code
moves, and testers do not read them); run-time binding when the page is not known statically (it
loses the unbound-step error before anything runs); callables that bind in the caller's scope
(every caller would repeat the callee's usings).

**Broken when:** a step binds to a phrase its file cannot see; a nearer phrase silently wins over a
wider one; a `Using` to nothing passes; a page-scoped step binds with no known page; a callable's
body changes meaning with the caller.
