# Design: the BDD preview in place of the Markdown preview (Visual Studio)

Status: **steps 1-4 built** 2026-09-28, not yet seen in Visual Studio. Date: 2026-09-28. From
the manual test, P-2.

**Spike result** (read from Visual Studio 2026's own assemblies):
- Its Markdown preview is the margin `MarkdownPreviewMargin` (right container, after
  `RightControl`, role `PRIMARYDOCUMENT`, content types `vs-markdown` and `vs-mermaid`), created
  only while the view option `ShowMarkdownPreview` is true
  (`DefaultMarkdownOptions.ShowMarkdownPreviewOptionName`, public). So 3a is simpler than
  `[Replace]`: the option is set to false on a feature file's view.
- The Markdown editor's content type is `vs-markdown`; `brinell-bdd` derived from `markdown`, a
  different type. So a feature file most likely opened as `vs-markdown`, and the LSP client, bound
  to `brinell-bdd`, may not have attached. `brinell-bdd` now derives from `vs-markdown`, and a view
  listener gives feature and step files `brinell-bdd` if they did not get it. It logs the content
  type before and after, and whether the preview was hidden, to the ActivityLog (source
  "Brinell BDD").

Built: `BddPreviewMargin.cs` (option, listener, margin), `PreviewController` driving several
controls, Alt+Shift+V toggling the margin, the guide and P-1/P-2. Q1 taken as its default: the
margin opens with the file.

## Problem

A feature file opens in Visual Studio's Markdown editor, which shows its own preview beside the
text: plain Markdown, no binding, no Run. Brinell's preview is a separate tool window (Alt+Shift+V)
that docks elsewhere or floats. A tester sees the wrong preview first, and has to find the right one.

## Goal

For `*.feature.md` and `*.steps.md`, the preview beside the text **is** the BDD preview: the same
place and feel as the Markdown preview, with the bar, the gutter, Run and Debug. Other `.md` files
keep Visual Studio's preview. The Scenario Preview tool window stays, for another screen.

## Design

1. **A right-hand editor margin.** An `IWpfTextViewMarginProvider` (MEF) for the `brinell-bdd`
   content type, `MarginContainer = Right`, text view role `Document`. It hosts a
   `ScenarioPreviewControl` (the WebView2 control the tool window uses), with a splitter and a
   remembered width.
2. **One controller, several views.** `PreviewController` today drives one control. It learns to
   drive the margin of the active editor as well as the tool window: the same host page, updates,
   caret sync, breakpoints and runs. Each margin shows its own file, not "the active one".
3. **Visual Studio's preview hidden for our files.** Tried in this order; the first that works
   is kept:
   - a. Replace Visual Studio's preview margin for `brinell-bdd` only (`[Replace]` with its margin
     name; the name and whether the editor allows it are found in the experimental instance).
   - b. `brinell-bdd` no longer derives from `markdown`. Visual Studio's preview, Markdown
     colours and toolbar go; our semantic tokens colour the steps. The prose loses Markdown
     colouring until the server colours it too.
   - c. If neither works: the docs say to turn off the preview in Tools > Options > Text Editor >
     Markdown (global).
4. **Commands.** Alt+Shift+V (and Ctrl+K, V) toggles the margin in the editor; the tool window
   stays under View > Other Windows. A setting (Tools > Options > Brinell BDD) says whether the
   margin opens with the file (Q1).

## Not changed

The language server, the preview's HTML and messages, the VS Code extension (it already opens its
preview beside the editor), and the tool window.

## Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | Spike 3a in the experimental instance: find the Markdown preview's margin name, try `[Replace]`. | 3a works or is ruled out, with the reason. |
| 2 | The margin (1) and the controller (2). | TOD.01 opens with the BDD preview beside it; P-1 to P-15 of the manual plan pass in the margin. |
| 3 | Hide Visual Studio's preview (3a, else 3b). | A feature file shows one preview; a plain `.md` still shows Visual Studio's. |
| 4 | Commands and the setting (4); the guide and the manual plan. | The guide describes the margin; the plan's P-checks name it. |

## Open questions

| # | Question | Default |
| --- | --- | --- |
| Q1 | Does the margin open with the file, or only on Alt+Shift+V? | Opens with the file, as the Markdown preview does. |
| Q2 | If only 3b works: is losing Markdown colouring of the prose acceptable? | Yes; the server colours headings and emphasis in a later step. |
