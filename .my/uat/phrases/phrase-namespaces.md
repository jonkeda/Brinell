# Phrase namespaces

Status: discussion
Date: 2026-09-26
Builds on: [phrase-scopes-and-composition.md](phrase-scopes-and-composition.md) (scopes: page,
file, module, global) and [callable phrases](phrase-scopes-and-composition.md#g-callable-scenarios)

The scopes doc says *that* phrases need scopes. This one asks how a scenario **gets at** phrases
that aren't in its own scope. Four questions:

1. Do we need a `using` statement?
2. What is a namespace, for phrases?
3. Phrases in separate projects: "create an account" and "create an invoice" may be owned by
   different teams, in different assemblies.
4. Phrases stored per folder, in a hierarchy.

## The example

Construction-sized, three areas owned by different teams:

```text
Scenarios/
  Accounts/            ACC.* journeys           team A
  Invoicing/           INV.* journeys           team B
    Credit notes/      INV.CN.* journeys        team B
  Projects/            PRJ.* journeys           team C
```

An invoicing scenario needs to **arrange** an account before it can invoice it:

```gherkin
Given an account "Acme" with payment terms 30 days
When I create an invoice for "Acme"
Then the invoice due date should be in 30 days
```

The first step is owned by Accounts, the other two by Invoicing.

"an account ... with payment terms" belongs to Accounts: that team writes it, fixes it when the
account screen changes, and knows what "payment terms" means. Invoicing *uses* it. The scopes doc
has no answer for this yet: the phrase is in a sibling module, not an ancestor.

## 1. A `using` statement

Yes. A scenario file declares which namespaces it draws phrases from, in its metadata, like
`TestData`:

```markdown
| Field | Value |
| --- | --- |
| Journey | INV.03 |
| TestData | one-account |
| Using | Accounts |
```

Without it, a scenario sees exactly what the scopes doc gives it: its page, its file, its module
(and its ancestor folders, see 4), and global. With it, it also sees Accounts.

Why explicit rather than "everything is visible":

- **The reader knows where a step comes from.** `Using | Accounts` at the top says "some of these
  steps are Accounts' phrases". That matters when Accounts changes one and the invoicing scenario
  breaks.
- **Collisions stay local.** Accounts and Projects can both have "I archive it". Only a file that
  uses both has an ambiguity, and it gets told at bind time, before anything runs.
- **The dependency graph is visible.** "Which scenarios depend on Accounts' phrases?" is a search
  for `Using | Accounts`. Accounts can change a phrase knowing who breaks.

Project-wide usings go in `uat.config.md`, like C#'s global usings:

```markdown
## Using

| Namespace |
| --- |
| Backend |
| Device |
```

That's where the subject phrases (backend, device, app lifecycle) would come from. Every file needs
them, and none should have to say so.

### What `using` does not do

- **Not transitive.** If Invoicing's phrases themselves use Accounts, an invoicing scenario doesn't
  see Accounts by using Invoicing. A file sees what it names. This keeps "where does this step
  come from" answerable by reading one file.
- **No shadowing.** Using a namespace never hides a phrase; two visible matches are an error (the
  scopes doc's rule).
- **Not per scenario.** One `Using` per file. A file is one journey, and the journey's needs don't
  change per scenario. Per-scenario usings would make the header lie about half the file.

## 2. What a namespace is

A namespace is **a name that owns a set of phrases**. Where the phrases physically are is a
separate question (3 and 4). The name is what `Using` refers to.

Short logical names (`Accounts`, `Invoicing`, `Backend`), not CLR namespaces
(`Exact.Construction.Uat.Accounts.Phrases`):

- Scenario files are read by testers. A CLR namespace is a developer detail, and it changes when
  code moves.
- One logical namespace may be fed from more than one place: C# phrase classes **and** markdown
  callable phrases, both owned by Accounts.

How each kind of phrase declares its namespace:

| Kind | Namespace from |
| --- | --- |
| C# phrase class | `[UatPhraseClass(Namespace = "Accounts")]`, or an assembly-level `[assembly: UatNamespace("Accounts")]` so a whole project is one namespace |
| Markdown callable phrase | the folder it's in (see 4), or a `Namespace` field in the `.phrases.md` metadata |
| Page phrase / control verb | the page. Pages are scopes, not namespaces. You don't `use` a page, you are *on* it |
| Global | no namespace: always visible, and kept small |

Should namespaces nest (`Invoicing.CreditNotes`)? Only as far as folders nest (4). A dotted name
is the folder path. Don't invent a second hierarchy beside the folders.

### Pages need the same treatment

Page names are global today (duplicate page names are a diagnostic). In a large app "Detail" or
"Edit" will collide just as phrases do. The same namespace that owns Accounts' phrases should own
its pages: `Given I am on the Account Detail page` resolves "Account Detail" among the pages of
the used namespaces. Same rule: ambiguity is an error, never a silent pick.

## 3. Phrases in separate projects

Namespaces are a **logical** grouping. Projects (assemblies) are **physical**. They should line up
but not be the same thing:

```text
Exact.Construction.Uat.Accounts.dll    [assembly: UatNamespace("Accounts")]
Exact.Construction.Uat.Invoicing.dll   [assembly: UatNamespace("Invoicing")]
Exact.Construction.Uat.Shared.dll      Backend, Device, AppLifecycle
```

What this gives:

- **Ownership matches team boundaries.** Accounts' phrases build and version with Accounts' page
  objects.
- **The engine loads what's declared.** `uat.config.md` already has an `## Assemblies` table
  (`Pages`, `Controls`, `Commands`, `TestData`). Phrase assemblies join it, each announcing its
  namespace.
- **Using is checked against what's loaded.** `Using | Accounts` with no assembly announcing
  Accounts is a bind-time error: "namespace 'Accounts' is not provided by any loaded assembly".

The C# side needs a dependency too: if an Invoicing phrase *calls into* Accounts' page objects,
that's a project reference. That's fine, but it's a code dependency, not a `Using`. `Using` is
only about which **phrase text** a scenario can write.

Risk: one project per module can mean a lot of small assemblies. It needn't: one
`Exact.Construction.Uat` assembly with `[UatPhraseClass(Namespace = ...)]` per class works just as
well. The namespace attribute makes the physical split optional.

## 4. Phrases per folder, in a hierarchy

The appealing version: phrases live next to the scenarios that use them, and folders nest.

```text
Scenarios/
  _phrases.md                    global callable phrases
  Invoicing/
    _phrases.md                  Invoicing
    INV.03-invoice-an-account.uat.md
    Credit notes/
      _phrases.md                Invoicing / Credit notes
      INV.CN.01-credit-a-line.uat.md
```

Rule: **a scenario sees the phrases of its own folder and every ancestor folder**, like
`.editorconfig` or `Directory.Build.props`. `INV.CN.01` sees Credit notes, Invoicing and global
without writing anything. That is the implicit using.

Pros:

- **Zero configuration** for the common case: a module's scenarios use the module's phrases.
- **Locality.** The phrase is next to the scenarios it serves. Deleting a folder deletes its
  vocabulary.
- **Specialisation reads naturally.** Credit notes adds phrases on top of Invoicing's, with no
  declaration.

Cons, and the trap:

- **Sibling sharing.** Accounts and Invoicing are siblings. With folders alone, the only way for
  Invoicing to reach Accounts' phrase is to **move it up** to a common ancestor. That's the root, so
  it becomes global and every scenario sees it. Done a few dozen times, the hierarchy collapses
  back into one flat global list, which is the problem we started with.
- **Moving a file changes its meaning.** Move a scenario to another folder and some of its steps
  may stop binding. The bind-time error catches it, but it is a surprise.

So folders alone aren't enough. They need `Using` for siblings.

### Folders and namespaces together

| Mechanism | Covers |
| --- | --- |
| Folder hierarchy | **implicit**: own folder plus ancestors. The common case, with nothing written |
| `Using` in the file | **explicit**: siblings and other projects (Invoicing using Accounts) |
| `## Using` in `uat.config.md` | **project-wide**: the subject phrases every file needs |

A folder *is* a namespace: `Invoicing/Credit notes` has the name `Invoicing.Credit notes` (or
`Invoicing/Credit notes`), and another folder can `Using` it. C# phrase classes join a folder's
namespace by declaring the same name. So a namespace is fed by markdown (by location) and C# (by
attribute), and there's only one hierarchy: the folders.

### Callable phrases bind where they're defined

One consequence for [callable phrases](phrase-scopes-and-composition.md#g-callable-scenarios): the steps
*inside* a callable phrase must bind in the **definition's** scope, not the caller's. That is
lexical scoping.

If Accounts defines "an account {name} with payment terms {days} days" using Accounts' own
phrases, an Invoicing scenario can call it without also using everything the phrase uses inside.
Otherwise every caller would have to repeat the callee's usings, and changing a phrase's inside
would break its callers.

## Qualified steps: an escape hatch?

When two used namespaces both have "I archive it", the file gets an ambiguity error. Options:

1. **Don't use both.** Usually the right answer: a journey rarely needs two teams' identical verbs.
2. **Rename one phrase.** "I archive the account" / "I archive the project". Better prose anyway.
3. **Qualify the step:** `When [Accounts] I archive it`.

Option 3 is the namespace-qualified name from programming languages, and it reads like code.
Provide it only if 1 and 2 turn out not to be enough in practice. It's easy to add later and hard
to take away.

## Tooling that makes this work

Namespaces only help if people can see them:

- **The bind report names the namespace** each step bound to (`Accounts: an account {name} ...`).
- **Unused `Using`**: a warning, like an unused C# using.
- **Unknown `Using`**: an error: the namespace isn't provided by any loaded assembly or folder.
- **The Presenter's step completion** offers only phrases visible from the file, grouped by
  namespace.
- **"Who uses this namespace?"**: `brinell-data`-style `references` check, listing the files
  with `Using | Accounts`, so a team can see who breaks before changing a phrase.

## Recommendation

1. **Namespaces are logical names**, declared by C# attribute or by folder, and not tied to CLR
   namespaces or assemblies.
2. **Folders give implicit visibility**: own folder and ancestors.
3. **`Using` in the file metadata** for siblings and other projects; **`## Using` in
   `uat.config.md`** for project-wide ones.
4. **Not transitive, no shadowing, ambiguity is an error.** Callable phrases bind lexically.
5. **Pages get namespaces too**, the same way.
6. **No qualified steps** until renaming and not-using-both prove insufficient.

## Open questions

| Question | Notes |
| --- | --- |
| Folder name as namespace, or a declared name per folder? | Folder names have spaces and can be renamed. A `Namespace` field in `_phrases.md` decouples them, at the cost of one more thing to keep in sync. |
| `Using` as a metadata row, or a `## Using` section in the file? | A metadata row matches `TestData`. A section allows comments on why. The row is probably enough. |
| Do C# phrase classes also see by hierarchy? | A C# class has no folder under `Scenarios/`. It only gets the namespace it declares. Probably right: C# is the developer side, and it states things explicitly. |
| Versioning between teams | If Accounts renames a phrase, the scenarios using it break. Should a phrase be able to keep an old text as a deprecated alias for a while? |
| Global scope: who decides what goes there? | Everything global is visible everywhere, forever. It should need a reason: the subject phrases, and generic control verbs. |
