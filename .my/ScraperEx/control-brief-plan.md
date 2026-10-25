# Controls the agent can make well: a brief per pattern, and recognizing what a pattern is

Status: **built**, steps 1 to 10 (2026-10-02, section 9); not yet tried in an IDE with an agent. From the Bouw7 trial
([issues/bouw7-trial-view-feedback.md](issues/bouw7-trial-view-feedback.md)): **Make 1 control with the
agent** on `div||v-input__control|div` gave a `VInputControl<TScope>` with constructors and nothing else,
after the agent had read many files to find out where and how to write it.

Two requirements from the owner:

- **The prompt says where to write and what to read.** The agent should not have to explore the
  repository.
- **No framework knowledge.** Recognition works from what any page has (tags, roles, ARIA, input
  types, structure, how occurrences differ), so it works for uncommon frameworks and an app's own
  custom controls. A UI library's class names are used only as names, never to decide what something is.
- **The model decides; the rules help.** Pass the agent everything known about a pattern. The rules
  of section 3 add their reading as **hints**, never as a filter or a verdict: a model reads a page
  better than a rule engine, and each makes the other better. Nothing the rules find is withheld
  because a rule did not like it.
- **Controls in a scraped project are generated** as in `srcnew`: a `.tpl.cs` with `*Core` methods,
  and the generator writes the `.gen.cs`.

## 1. Why the control was empty

**The pattern was three controls.** `v-input__control` is an inner wrapper that every field of the
page has. Across the Bouw7 captures it occurs as:

| Its parent's classes | Inside | Count | Really is |
| --- | --- | --- | --- |
| `v-input v-text-field v-select …` | read-only text input, hidden input, `aria-haspopup="listbox"`, an icon | 216 | a select |
| `v-input v-text-field …` | editable text input | 71 | a text field |
| `v-input v-input--checkbox …` | `input type=checkbox` | 2 | a checkbox |

The parents show up as `VInput`, `VInput2`, … because each mix of modifier classes is a signature of
its own.

**The prompt carried nothing to build on.** It sends a signature, a count, one node number, and "make
these controls, one file each, with a scraped header, check_code until it passes". It does not say
what a control offers, what is inside the pattern, where the file goes, or which files to read. So the
agent read around the repository, and the html-control skill allows a control with only constructors.

## 2. What the Scraper knows today

| Where | What |
| --- | --- |
| `PatternDetector` | Patterns by `NodeSignature` (type, role, stable classes, child types). Kinds: `Form`, `Table`, `List`, `Navigation`, `Dialog`, `Frame` by tag or role; else `Repeated`. A name per pattern. |
| `ControlAdvisor` | A page member per element, with the Bruinell control that fits: `input` → `TextInputControl`, `select` → `SelectControl`, checkbox → `CheckBoxControl`, `table` → `TableControl`, `ul`/`ol` → `ListControl`, `role=tablist` → `TabContainerControl`, … |
| `ControlChecker` | For a made control: each part's locator, found in how many of the pattern's occurrences. |
| Explore's log | What each click did: a page change, a new capture, a dialog, nothing. |
| MCP | `get_page_snapshot`, `find_similar_elements`, `suggest_controls`, `scraped_header`, `check_code`. |

What is missing: knowing **what a repeated part is**, and **saying it**, with where it goes, to the agent.

## 3. Recognizing what a pattern is, without framework knowledge

Everything here reads a snapshot's nodes: tag, role, attributes (ARIA, `type`, `readonly`,
`disabled`, `tabindex`, `id`, `for`), text, children, and the pattern's other occurrences. No list of
library classes.

These rules produce **hints** for the brief (section 4): a core, a kind, a root, variants, parts. The
facts they read go into the brief as well, so the model can disagree. Where a rule has no answer, it
says so and the model decides; the person is not asked.

### 3.1 The interactive core: what a test does with it

First, each occurrence's **interactive descendants**, each classed by generic signals:

| Core | Signals |
| --- | --- |
| **text** | `input` of a text type (text, email, number, search, tel, url, password) or `textarea`, not `readonly`; `contenteditable`; `role=textbox`. |
| **selector** | `select`; `role=combobox/listbox`; `aria-haspopup=listbox/menu/tree/grid`; an input or button with `aria-expanded` and `aria-owns`/`aria-controls` pointing to a list; a `readonly` input inside a clickable box. |
| **toggle** | `input type=checkbox/radio`; `role=checkbox/switch/radio/menuitemcheckbox`; `aria-checked`, `aria-pressed`. |
| **range** | `input type=range`; `role=slider/spinbutton`; `aria-valuenow`; `progress`, `meter`. |
| **date/time** | `input type=date/time/datetime-local`. |
| **clickable** | `button`, `a[href]`, `role=button/link/menuitem/tab/option`, `input type=button/submit`, `summary`; an element with `tabindex>=0` and a click seen in Explore's log. |
| **readable** | text a test checks: headings, labels (`label`, `legend`, `aria-label`, `aria-labelledby`), status (`role=status/alert`, `aria-live`), messages next to an input. |

A custom control with no ARIA (a `div` that opens a list on click) is caught by **behaviour**: Explore
clicked it and a popup or list appeared, or the element has `tabindex` and a pointer handler. When
nothing tells, the core is **unknown**, and the brief says so with the outlines and the behaviour
seen: the model decides from those.

### 3.2 The kind

| Kind | Bruinell base | From the core and structure |
| --- | --- | --- |
| **Simple control** | the family of its one core: `TextInputControl`, `SelectorControlBase`, `ToggleControlBase`, `RangeControlBase`, `DateInputControl`, `ClickableControlBase` | exactly **one** interactive core, plus readables that belong to it (its label, hint, error, icon) |
| **Component** (container with parts) | `ContainerBase<TParent, TSelf>` | **two or more** cores or readables, each found by the same relative locator in every occurrence: a card with a title and buttons, a toolbar |
| **Collection with rows** | `CollectionObjectBase` + `ItemObjectBase` | a parent with **two or more children of one signature** (or grandchildren under one wrapper, as `tbody > tr`); the children are the rows. `role=row/listitem/option/gridcell` confirms it, but is not needed. The row's parts are its members; the key is a `data-*` value or a numbered `id` that differs per row, else the first readable's text |
| **Table** | `TableControl`, or a collection of rows | `table`, `role=grid/table`; the header cells (`th`, `role=columnheader`) name the columns |
| **Tabs** | `TabContainerControl` | `role=tablist`, or siblings of one signature where exactly one has `aria-selected`/`aria-current` or a class the others lack |
| **Dialog** | `ContainerBase` with `AsksParent => false` | `role=dialog/alertdialog`, `aria-modal`, `dialog`; or a block that appeared after a click (Explore's log) over the page |
| **Frame** | `FrameScope<TParent>` | `iframe` only. A shadow root (Bouw7's `b7-*` elements) is a container: Playwright's CSS reaches into open shadow roots, XPath does not, so parts inside one get CSS locators |
| **Form** | page object members, not a control | `form`, `fieldset`: the page names its fields |

### 3.3 Where the control starts: the root, found by structure

A pattern's node is often an inner wrapper (`v-input__control`), and the thing to wrap is an ancestor
that also holds the label and the messages. Walk up from the node; an ancestor is still the same
control while, in **every** occurrence:

1. it holds **one** occurrence of the pattern (not a form row with three fields);
2. what it adds is only **readables** of that core (a label, a hint, an error, an icon), no other
   interactive core;
3. it is not a landmark (`form`, `fieldset`, `section`, `main`, `nav`, `body`) or a row of a
   collection (3.2).

The root is the highest such ancestor. For Bouw7 that is the parent with the classes `v-input …`:
it adds the `legend` and the messages, and its own parent holds a second field. No knowledge of
Vuetify is needed.

### 3.4 Variants: when one pattern is several controls

Group the occurrences by their **core** (3.1). When the groups differ, the pattern is probably several
controls: **To make** offers one row per variant, each at its own root (Bouw7's wrapper becomes three:
selector 216×, text 71×, toggle 2×), and the brief shows every variant with an example, so the model
can still make one control with a mode, or merge two.

The roots' signatures leave out classes that differ inside a variant (modifiers, state), so `VInput`,
`VInput2`, … merge into the variant they belong to.

**Names** stay generic: a class that every occurrence of this variant has and no other variant has
(`v-select`, giving `VSelect`); else the root's naming class plus the core (`VInputText`,
`VInputToggle`). The person renames in the view.

### 3.5 Parts: what a component or row offers

For a component or a row: its cores and readables, each with a **relative locator** that finds it in
every occurrence. This is `ControlChecker`'s check, done before the control is made. A part found in
fewer occurrences is optional, or a sign of two variants (3.4).

### 3.6 The person's word wins

The config can name an app's own components where the signals fall short:

```
## Components

| Match | Kind | Name |
| --- | --- | --- |
| selector: .heimdall-table | collection | HeimdallTable |
| selector: b7-navbar-button | clickable | NavbarButton |
```

### 3.7 Nothing hidden

Today `PatternDetector` leaves out a wrapper with no class, role or shared id (`div|||div`) and a
layout cell (`td.pb-4`), so the view does not offer them as controls (done 2026-10-02, the naming
follow-up). Under this plan that is a hint too: they stay out of **To make**, but **All patterns**
lists them with "a bare wrapper" or "layout only", and a brief can be asked for any of them.

## 4. The brief

The server writes a brief per pattern (`bruinell/scraper/controlBrief`, and an MCP tool
`control_brief`). It is in three parts:

- **Task**: where to write, what to read, and nothing else to look for.
- **Facts**: everything recorded about the pattern, as data: every variant with a count and an
  example outline that includes the root's parent, each occurrence's page and node, the attributes
  that matter (ARIA, `type`, `readonly`, `disabled`, `id`, `for`, `data-*`), texts, the parts with the
  relative locator and in how many occurrences it finds them, and what Explore saw when it clicked
  an occurrence.
- **Hints**: what the rules of section 3 make of the facts: core, kind, root, base, members. Marked
  as hints, each with the facts it rests on.

One brief, to show the format. Only node 138's structure and the variant counts are real; the other
node numbers, the part counts, the texts, the list's options and the Explore line are made up:

```
## VSelect (216× on 4 pages)

### Task
Write:      E:\…\Scraper.Sites\Bruinell.Html.Bouw7\Controls\VSelect.tpl.cs   (new file; the generator writes VSelect.gen.cs)
Namespace:  Bruinell.Html.Bouw7.Controls
First line: // <scraped control="…" from="…" page="/department#" node=138 project="Bruinell.Html.Bouw7">
Read only:  .github/skills/html-control/SKILL.md
            srcnew/Bruinell.Html/Controls/SelectorControlBase.tpl.cs   (the hinted base: its *Core methods)
            srcnew/Bruinell.Html/Controls/Selection/SelectControl.tpl.cs   (a control of the hinted kind)
Check:      check_code (it runs the generator over Controls, then builds)

### Facts
Pattern node: div.v-input__control (an inner wrapper). Its parent in every occurrence: div.v-input.
Variants of the pattern (by what is inside):
  216×  readonly input + hidden input, aria-haspopup=listbox      e.g. /department# node 138
   71×  editable input type=text                                   e.g. /invoice# node 412
    2×  input type=checkbox                                        e.g. /department#/create node 97
This brief is for the first variant.

Example (/department#, node 138, with its parent):
  div .v-input.v-select.v-text-field.v-input--dense …
    div .v-input__control
      div .v-input__slot  aria-haspopup=listbox aria-expanded=false aria-owns=list-129
        fieldset > legend > span
        div .v-select__slot
          div .v-select__selections > input #input-129 placeholder="Select" readonly
          div .v-input__append-inner > div .v-input__icon > i aria-hidden
      div .v-menu
  The list it owns (list-129) is at the end of body: div role=listbox > div role=option × 6 ("Bouw", "Installatie", …)

Parts (relative to the root), found in:
  input:not([type=hidden])      216 of 216   readonly, placeholder "Select"
  .v-select__selections         216 of 216   text: the chosen item, empty in 180
  [aria-haspopup]               216 of 216   aria-expanded=false
  legend, label                 140 of 216   texts: "Afdeling", "Type", …

Explore: clicked [aria-haspopup] on /department# : aria-expanded became true, list-129 showed 6 options.

### Hints
Core: selector (aria-haspopup=listbox; a readonly input; Explore saw a list open).
Kind: simple control. Base: SelectorControlBase<TScope>.
Root: the parent div.v-input (it adds the label; its own parent holds other fields).
Members: SelectItem(text) — open, then the role=option with that text in the list aria-owns names;
         GetSelectedText() — the selections' text; Open() — confirm aria-expanded=true;
         GetLabel() — optional (140 of 216); IsEnabled — no disabled/aria-disabled on the input.
```

For a collection the facts hold the row signature, the rows' count per page, the row's parts with
their counts and the candidate keys; for a table, the header texts; for a component, each part with
its own core.

### 4.1 Budget

The agent's whole job stays well under 64k tokens: some models in Visual Studio's Copilot have small
windows, and besides the brief the agent reads files (the skill, the base and an example control are
about 10k characters, some 2.5k tokens), reasons, writes the code and reads `check_code`'s output,
often more than once.

| Part | Limit |
| --- | --- |
| Task and hints | always in full (about 500 tokens) |
| Variants | all, one line each, up to 10; the rest as "+N more variants, M×" |
| Example outline | about 80 lines for the variant being made, about 30 for each other variant; only the attributes that matter |
| Parts | all, one line each, up to 30 |
| Owned list | its first 10 options |
| Explore's clicks | up to 5 |
| **One brief** | **at most about 6k tokens** (some 24k characters, counted as characters / 4) |
| **One prompt** | **at most 5 controls** (about 30k tokens); more ticked, the view sends them in batches of 5 |

What is cut stays reachable: the brief ends with `more: control_brief(pattern, variant)` and the node
numbers for `get_page_snapshot`. The numbers are a start, to tune on real briefs from Bouw7.

## 5. The prompt

Per control, the brief, and these instructions in place of the one line now:

1. **Read only the files under "Read only".** Do not search the repository: the brief has the facts.
   Use `get_page_snapshot` only to look at an occurrence again.
2. **Decide what the control is from the facts.** The hints are a reading of them, not an order:
   where you see it differently (another base, one control for two variants, other members), say
   why, and do that.
3. **First say in a few lines** what the control will offer: base, parts, members. Then write it.
4. **Write the template at "Write"**, in "Namespace", "First line" first: constructors, a
   `protected virtual` `*Core` method for each thing a test reads or does, hand-written members only
   where the skill says so. For a component, a control per part. Not only constructors.
5. **"Check"** until it passes; report each member, and any part it could not find in every
   occurrence.

**Fix with the agent** sends the brief again, with the counts the check found for the made control.

## 6. Steps

| Step | What | Where |
| --- | --- | --- |
| 1 | The generator over a scraped project: `check_code` and the draft step run `bruinell controls generate --input <project>\Controls` before they build | `ScraperService` (check), `ProjectCode`; the generator itself takes any folder already |
| 2 | The brief: task, facts (variants, outlines with the parent, attributes, parts with counts, the owned list, Explore's clicks), hints | `Bruinell.Scraper/Code/ControlBrief.cs`, `bruinell/scraper/controlBrief`, MCP `control_brief` |
| 3 | The prompt: the briefs and the instructions of section 5, at most 5 controls per prompt (4.1); the `scrape-pages` skill's step 4 to match | `ScraperPage.cs` (`makeControls`, **Fix with the agent**), `.github/skills/scrape-pages/SKILL.md` |
| 4 | The hints: core and kind (3.1, 3.2) as one classifier, used by the brief and by `ControlAdvisor` | `Bruinell.Scraper/Code/ControlKind.cs` |
| 5 | The root by structure (3.3); variants (3.4) with generic names | `PatternDetector`, `NodeSignature` |
| 6 | Collections (3.2): siblings of one signature as a collection with its row and key | `PatternDetector` (a `Collection` kind) |
| 7 | Behaviour: Explore's clicks as facts (what changed: `aria-expanded`, a new list, a dialog) | the explore report, `ControlBrief` |
| 8 | Owned lists: follow `aria-owns`/`aria-controls` to the list outside the control, and its options | `ControlBrief`, `ControlKind` |
| 9 | `## Components` in the config (3.6); bare wrappers listed with a hint (3.7) | `ScraperConfig`, `PatternDetector` |
| 10 | The agent and the model (7a): a `bruinell-scraper.agent.md`, the choice in the header, sent with each prompt; `chatDone` refreshes the step | `.github/agents/`, `ScraperPage.cs`, `scraper.ts`, `ScraperController.cs`, `CopilotChatSender.cs` |

Steps 1 to 3 would have given the agent the place, the facts and a generated control for Bouw7.
Steps 4 to 9 sharpen the hints and what the view offers, so they follow once 1 to 3 are tried.

## 7. How to know it works

- **Bouw7.** **To make** offers a selector (216×), a text field (71×) and a checkbox (2×) at the
  `v-input` root, and `HeimdallDataRow` as the row of a collection; no `VInputControl` or numbered
  `VInput`.
- **The agent.** From the brief, the selector is a `.tpl.cs` deriving from `SelectorControlBase`,
  with `*Core` methods for selecting an item and reading the selection; the generator gives it
  `SelectItem`, `GetSelectedText`, `Wait…`, `Assert…`; its parts are found in all occurrences (step
  5's **Made** table). Its run reads only the files the brief names.
- **No framework knowledge.** The same quality on the other `Scraper.Sites` projects (React, Vue 2,
  plain, Blazor), and on a page of custom controls with no ARIA, added to the test site.
- **Tests.** The classifier on snapshots of each core and kind; the root on the Bouw7 field and on a
  form row with three fields; the owned list through `aria-owns`; the brief for a select, a row and
  a card; `check_code` generating a scraped project's `.gen.cs`.

## 7a. Which agent and which model

The person chooses the chat agent (or mode) and the model the Scraper's prompts run with.

**What each IDE allows** (checked 2026-10-02):

| | VS Code | Visual Studio |
| --- | --- | --- |
| Send, not only type | `workbench.action.chat.open` with `isPartialQuery: false` | the session's `SendRequestAsync` (done, see the Bouw7 feedback issue) |
| Agent or mode | `mode`: `agent`, `ask`, `edit`, or a custom agent's name (`.github/agents/*.agent.md`) | `CopilotRequest.DirectedResponders`: a responder from the session's `GetRespondersAsync`. Whether Visual Studio's custom agents are responders is not known yet |
| Model | `modelSelector: { vendor, family, id }` (an error when none matches) | `CopilotRequest.Model = new CopilotModelRequest(family)`, with `ReasoningEffort`; the list from the session's `GetDefaultModelsAsync` |
| Tools | `toolsInclude: [...]`: only the Bruinell MCP server's | not per request |
| Know when it is done | `blockOnResponse: true`: the command returns when the answer is complete | the request's task completes |

**A Bruinell agent.** `.github/agents/bruinell-scraper.agent.md`: the `bruinell-bdd` tools only, and as
its instructions the `scrape-pages` skill's rules and section 5's. The prompt then needs only the
brief. In VS Code it is chosen with `mode: "bruinell-scraper"`. In Visual Studio it is used when it
shows up as a responder; otherwise the request goes to the default agent with the same instructions
in the prompt.

**Choosing.** In the view's header, by **Next**: **Agent** (Bruinell scraper, Agent, Ask) and
**Model**. The IDE gives the list of models it has (VS Code `vscode.lm.selectChatModels()`, Visual
Studio `GetDefaultModelsAsync`). The choice is remembered per person in `.scraper-state.json` (not
committed). `scraper.config.md` may set a default for the project:

```
## Agent

| Field | Value |
| --- | --- |
| Agent | bruinell-scraper |
| Model | claude-sonnet-4.5 |
```

A model the IDE does not have falls back to the IDE's default, and the notice says so.

**Done means refresh.** Both IDEs can tell when the answer is complete (`blockOnResponse`, the
request's task). The host then sends the page `{type: "chatDone"}`, and step 5 or 6 checks again by
itself: **Made** fills in without **Check again**.

**Messages.** The page sends `{type: "chat", prompt, agent, model}`. The host answers
`{type: "chatted", via, agent, model}` with what it used, and `chatDone` at the end. The page asks
for the lists with `{type: "agents"}` and gets `{type: "agents", agents, models}`.

**Steps.** Added to section 6 as step 10: the `.agent.md`; the header's choice and its memory; the
hosts passing `mode`/`modelSelector`/`toolsInclude` (VS Code) and `Model`/`DirectedResponders`
(Visual Studio); `chatDone`. It does not wait for steps 1 to 9.

## 9. Built (2026-10-02)

| Steps | Commit | Where it differs from the plan |
| --- | --- | --- |
| 1 | `bf8f515a` | The generator stays in the CLI; the Scraper gets it as an `ITemplateGenerator`. It runs over the project's `Controls` and `Pages` folders before every build (`check_code`, drafting). |
| 2 to 5 | `c045748b` | One commit: they share their files. A lifted pattern keeps the **inner** signature (all its occurrences share it; the roots' own classes vary, as Bouw7's `has-c`), and a root that several patterns reach goes to the one with the most occurrences. The header records `variant` and `root="lifted"`; `ControlChecker` lifts and filters the same way. The root rule (3.3) also lifts to the BEM block of the node's element class (`x__y` inside `x`). The inner-pattern rule now applies only inside a repeated part, not a form or table. |
| 6 to 8 | `3f2c6d77` | Explore's clicks come from its reports (`explore.md`, the click's label); what appeared is the popup roles that grew between the capture before and after. |
| 9 | `1c05dfc9` | |
| 10 | (this commit) | Visual Studio cannot choose an agent or mode through its API: it sends into the open chat session, in the window's own mode, with the chosen model (`CopilotRequest.Model`). VS Code sends with `mode`, `modelSelector` and `blockOnResponse`; a VS Code that does not wait for the answer (it returns within 2 s) gives no `chatDone`. The agent file's `tools` names (`search`, `edit`) are not checked against a VS Code. |

On the Bouw7 recordings: `VSelect` (62×, selector, lifted), `VInputText` (16×), `VInputLabel` (4×)
instead of `VInputControl` and `VInput`..`VInput4`; `HeimdallDataRowList` with its rows; no `Div` or
`Td`. Not yet tried: an agent making a control from a brief, in Visual Studio and VS Code.

## 8. Decisions and open questions

Decided 2026-10-02 (the owner):

- **The generator may change**, and runs over a scraped project's `Controls` (step 1).
- **A select's options are part of the control**: it follows `aria-owns`/`aria-controls` to the list
  the page renders elsewhere (step 8). Where no attribute links them, the facts show where the list
  appeared after a click, and the model decides.
- **Unknown is for the model**: the person is not asked for a control's kind; the brief gives the
  model the facts.
- **As much information as there is goes to the model**; rules give hints and support it, never
  replace it.
- **Shadow roots are containers.** An open shadow root (Bouw7's `b7-*` elements) needs no scope of its
  own: Playwright's CSS locators reach into it. A closed one is not recorded at all, so there is
  nothing to make. XPath does not reach into a shadow root, so a part inside one gets a CSS locator;
  the brief says when a part is inside one.
- **A brief has a budget** (section 4.1).

Open:

- Visual Studio: are custom agents (a `.agent.md`) responders a request can be directed to? Try it with `GetRespondersAsync` once the `.agent.md` exists (step 10).
