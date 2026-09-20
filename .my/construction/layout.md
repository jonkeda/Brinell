# Construction UI test migration - target layout

This is the file/folder shape the migration produces, all **inside the existing Construction
repo** (`E:\repos\MainConstruction\construction`). The two host projects already exist;
Todos and later modules only add folders and files under them.

```text
Exact.Core.Construction.UITest/              # ControlObject library (Brinell-Construction bridge) - EXISTS
  Exact.Core.Construction.UITest.csproj
  Controls/
    Search/            PageSearchControl.tpl.cs + .gen.cs          # seed, exists
    Loading/           LoadingOverlayControl.*                      # seed, exists
    Messaging/         InlineAlertControl.*                         # seed, exists
    HeaderActions/     PageHeaderActionsControl.*                   # seed, exists
    TitleBar/          PageTitleBarControl.*                        # seed, exists
    Lists/             EmptyStateControl.*                          # seed, exists
    Favorites/         FavoriteToggleControl.*                      # seed, exists
    Collection/        PageableCollectionControl.*                  # seed, exists
    FormInputs/        TextFormInputControl.*, NoteFormInputControl.*,   # phase 1, new
                       DateTimeFormInputControl.*, SelectionFormInputControl.*,
                       (later) NumberFormInputControl.*, BooleanFormInputControl.*, ...
    FormSections/      FormContainerControl.*, FormSectionControl.*     # phase 1, new
    Loose/             RoundButtonControl.*, TabMenuItemControl.*,      # phase 3, converted
                       SearchOverlayControl.*, ToggleRowControl.*        # from UITests/Controls/
    Cells/             per shape, added on demand
    QuickActions/      QuickActionsControl.*
    DisplayFields/     per shape, added on demand
    Rich/              RichContentControl.*, RichEditorControl.*   (phase 6)
    Settings/          per shape                                    (phase 6)
  PagePatterns/
    OverviewPagePatternObject.cs                                    # exists
    DetailPagePatternObject.cs                                      # new (first module needing it)
    EditPagePatternObject.cs                                        # phase 1 (= T4)

Exact.Construction.UITests/                  # single xUnit test host - EXISTS
  Exact.Construction.UITests.csproj          # references Exact.Core.Construction.UITest;
                                             # Brinell via eng/brinell/Brinell.references.props
  GlobalUsings.cs
  ConstructionFixture.cs                     # exists - base fixture (login, app launch)
  ConstructionTestBase.cs                    # exists
  ConstructionCollection.cs                  # exists
  SetupFailedException.cs
  TestConstants.cs
  brinell.maui.config.json                   # exists - resolves the app path
  construction.maui.config.json              # exists
  Auth/                                       # exists - REUSE for login
    ConstructionAuthenticationManager.cs
    Pages/                                     # login page objects
    ...
  Configuration/                              # exists
  Diagnostics/                                # exists
  MockBackend/                                # exists - WireMock host
  TestData/                                   # exists
  TestSettings/                               # exists
  Controls/                                   # 4 loose controls; emptied as C6-C9 convert
    RoundButtonControl.cs, SearchOverlay.cs, TabMenuItemControl.cs, ToggleRow.cs
  Modules/
    Authentication/                           # exists
    Contacts/                                 # exists - Containers/ Pages/ Tests/
    Dashboard/                                # exists
    Startup/                                  # exists
    Todos/                                    # NEW (PoC, phase 2)
      TodosCollection.cs
      TodosFixture.cs                          # inherits ConstructionFixture
      Pages/
        ProjectTodosOverviewPage.cs
        TodoInspectionPage.cs
        TodoEditorPage.cs
      Containers/
        TodoListCollection.cs
        TodoItemContainer.cs
      Tests/
        TodoOverviewSmokeTests.cs
        TodoInspectionSmokeTests.cs
        TodoEditorSmokeTests.cs
    <Module>/                                 # one folder per module as it is picked up
      <Module>Collection.cs
      <Module>Fixture.cs
      Pages/  Containers/  Tests/
```

## Project references

There is **one** test project (`Exact.Construction.UITests`) and it already references what
it needs:

- `..\Exact.Core.Construction.UITest\Exact.Core.Construction.UITest.csproj` (ControlObjects,
  page-pattern bases).
- Brinell packages via `<Import Project="..\eng\brinell\Brinell.references.props" />` plus
  the `IncludeBrinellHtml` / `IncludeBrinellCommunityToolkit` / `IncludeBrinellFlaUI` flags.

No new projects are created for modules. A module is a folder under `Modules/`.

`Exact.Core.Construction.UITest` (the library) references only Brinell packages. It has
**no** reference to the test host project.

## Solution wiring

Both projects are already in `Construction.sln`. Adding a module folder needs no solution
change. Adding a new file to an SDK-style project needs no `.csproj` edit either.

## The Construction app itself

Built from source in the same repo. The tests launch it via the existing
`brinell.maui.config.json` / `construction.maui.config.json`. Rebuild the app when
`Exact.Construction` changed:

```powershell
dotnet build Exact.Construction\Exact.Construction.csproj -f net10.0-windows10.0.19041.0
```

## Namespaces

| Location | Root namespace |
| --- | --- |
| `Exact.Core.Construction.UITest` | `Exact.Core.Construction.UITest` (controls under `.Controls.<Folder>`, page patterns under `.PagePatterns`) |
| `Exact.Construction.UITests` (host) | `Exact.Construction.UITests` (auth, diagnostics, mock backend, configuration each in their own child namespace) |
| `Exact.Construction.UITests/Modules/<Module>` | `Exact.Construction.UITests.Modules.<Module>` with `.Pages`, `.Containers`, `.Tests` children |

Matches the folder shape of the existing `Modules/Contacts` so new module files change only
their `using` list and namespace declaration.
