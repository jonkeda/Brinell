# Bouw7 trial: feedback on the Scraper view

Status: implemented 2026-10-02, not yet retried in Visual Studio. Found 2026-10-02, running [bouw7-vs-test-plan.md](../bouw7-vs-test-plan.md) in Visual Studio.

The view is the page in `tools/Bruinell.Cli/Lsp/ScraperPage.cs`, shared by Visual Studio and VS Code.

## 0 General

**a. Step actions scroll out of sight.** Buttons such as **New project…** and **More** sit at the
bottom of the step's content, so they scroll away. They should be below the header, where they
are always visible.

- Now: `sc-actions` is part of the step body, rendered into `sc-panel`.
- Direction: a fixed action bar at the top of the panel, under the step's heading, for every step.

## 1 Project

**a. Platform and Start URL cannot be edited.** Step 1 shows them read-only (`stepProject`). They
should be editable and saveable there.

- Now: the Start URL can be edited only in step 2, under **More**. The Platform only in
  `scraper.config.md`.
- Direction: editable fields with **Save** in step 1, through `configEdit` (as step 2 does with
  `set App StartUrl`). Then drop the copy under step 2's **More**.

## 5 Controls

**a. A bad control name: `Div` for `div #frame-content`.** It should be `FrameContent`.

- Cause: `controlName(signature)` in `ScraperPage.cs` builds the name from the signature's class
  part and otherwise its type. The signature has no id, so an element with only an id gets its
  tag name.
- The server already has a better namer: `ControlAdvisor.Name` (test id, then text, placeholder,
  `name`, `id`, `title`, `data-*`, and only then the type).
- Direction: the server suggests the name with each pattern, from the example node, using that
  namer. The page shows it.

## 3 Collect pages

**a. Guards and budget take up the step.** The form should be inside an expander, collapsed once
it has been saved.

**b. Record by hand is hidden.** It is under **More**. It should always be visible, next to
**Explore**.

**c. The Never click / Always click buttons are not clear.** Nothing on screen says what they do.

- What they are: lists in `scraper.config.md` that overrule Explore's own judgement of an
  element. **Never click** means Explore never clicks it, even if it looks safe (for example
  *Empty cache*). **Always click** means Explore clicks it whatever its words say (for example a
  *Filters* toggle). Never wins over always (`ClickJudge`).
- They appear in three places: the guards form (`Never click…`, `Always click…`, which open the
  list), the skipped list in step 4, and the node menu (`Never click this`, `Always click this`).
- Direction: label them for what they do, e.g. "Elements Explore must not click (n)" and "Elements
  Explore may always click (n)", with a one-line hint. Show the same hint in step 4.

**d. No view of what is being collected.** While Explore runs, or during Record by hand, there
is only the progress strip.

- Question: should a page list fill in as pages are captured, with each new page appearing and
  the latest one highlighted?
- Now: after Explore the view jumps to step 4 (Review). During Record by hand nothing changes
  until you open step 4.
- Direction: step 3 shows a live list of captured pages (key, captures, the click that led
  there) for both Explore and Record by hand. Step 4 stays the place to look closer.

## Done

- **0a**: the step's first action row is moved under its heading as a bar that sticks below the
  header (`.sc-bar`). The header, the progress strip and the error message now stick together on top.
- **1a**: step 1 has a Platform choice and a Start URL field with **Save** (`configEdits`, `## App`).
  Step 2 shows the Start URL with a link to step 1.
- **5a**: `PatternDetector` names each pattern (`Pattern.Name`). It uses a test id or id that all
  occurrences share, with numbers dropped (`row-1`, `row-2` gives `Row`). Otherwise the first
  stable class, the role, or the type. The page uses that name.
- **3a**: guards and budget sit in an expander, collapsed, with a one-line summary.
- **3b**: **Record by hand** is next to **Explore**. Its session and start URL stay under More.
- **3c**: the lists are labelled by what they do ("Elements never clicked (n)…"). Each has a hint
  as a tooltip and on its own page. Step 4's buttons say "Never click it" / "Always click it",
  with a line explaining them.
- **3d**: step 3 lists what this run collected, newest first and highlighted, as captures arrive.
  This works for Explore and for Record by hand. A row opens the capture in step 4. The view no
  longer jumps to step 4 when Explore ends.

Checked in a browser via the stand-in host: the bar stays under the header while scrolling, the
step 1 Save works, and step 3's layout and labels are right. The live list (3d) is not tried yet:
it needs a recording.

## Follow-up: names still wrong (`Div` for `div|||div`)

`div|||div` (13× on 5 pages) did not stand for one element. It grouped every classless div with
one div child: unnamed wrappers, `#workspace` and `#frame-logo`. Nothing was shared, so the name fell
back to the type. Class names had a second problem: the name came from the alphabetically first class,
giving `Hydrated`, `ThemeLight`, `Px`, `Pb`, `HasC`.

Now (`PatternDetector`):

- A pattern with nothing that says what it is is not offered as a control. That means no naming class,
  no role, no shared id, and not a custom element. This covers bare wrappers and layout cells such as
  `td.pb-4`.
- A custom element is named after its tag (`b7-navbar-button` gives `B7NavbarButton`).
- Otherwise the name is the first class in the element's own order that is not a modifier (`--`) or a
  utility or state class (`pb-4`, `d-flex`, `theme--light`, `hydrated`, …).
- Patterns that end up with the same name are numbered (`VInputSlot2`).

On the Bouw7 recordings the list now reads `VInputControl`, `B7NavbarButton`, `VBtn`, `VCard`,
`VDataFooter`, `HeimdallDataRow`, `FrameLogo`, `FrameContent`, … with no `Div` or `Td`.

## Follow-up: "The agent makes 1 control", and nothing happened

Neither IDE sends the prompt. Visual Studio puts it on the clipboard (Copilot Chat has no command
that takes a prompt) and says so only in its status bar. VS Code types it into the chat without
sending it. The view still claimed the agent was at work.

Now each IDE answers the page with `{type: "chatted", via, opened}`, and Visual Studio also opens
Copilot Chat's window (`TryOpenCopilotChat`: the `View.*Copilot*Chat*` command). The page shows a
notice that stays until closed: where the prompt is, that you still send it yourself, and for
controls, a **Check again** button. The notice was checked in a browser; Visual Studio opening the
chat window was not tried yet.
