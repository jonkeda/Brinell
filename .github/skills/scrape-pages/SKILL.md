---
name: scrape-pages
description: Make Brinell Html or Blazor page objects and controls from an app's recorded pages - record the pages with the Scraper into a UI test project, find the repeated parts, write controls first and pages second, and build until it passes. Use when asked to scrape, record or crawl an app or site for page objects, to generate page objects or controls from a web app, or to update them after the app changed.
---

# Page objects from recorded pages

The Scraper records an app's pages into a UI test project (`Recordings/`, beside the code) and
answers questions about them through the Brinell MCP server. It calls no model: you do the
writing, it holds the pages, finds patterns and runs the gate. The conventions are the
[html-control](../html-control/SKILL.md) and [html-ui-test](../html-ui-test/SKILL.md) skills:
read them first.

## Workflow

1. **The project.** `list_scraped_projects`. A project is scrapeable when it has a
   `scraper.config.md` (`## App`: `Platform` html or blazor, `StartUrl`, `Namespace`, `Pages`,
   `Controls`, `Screenshots`; `## Aliases`, `## Test ids`, `## Masks`). None: ask which UI test
   project, and propose the file.
2. **Record.** `start_recording` with a session name opens a browser window the *user* works in.
   Each page the app moves to is captured once it settles; `capture_page` with a name captures
   one more state (a dialog open, a tab chosen). `stop_recording` when they are done. Never sign
   in or type credentials for the user: the browser keeps the project's own login.
3. **Look before writing.** `list_controls` (reuse what exists, in this project and the scraped
   projects it references), `list_recorded_pages`, `detect_patterns`. A `Repeated` pattern on
   several pages, or several times on one, is a control to make; `Form`, `Table`, `List`,
   `Navigation` and `Dialog` are what a page object names. `get_page_snapshot` gives a page as an
   outline with node numbers; ask for a `node`'s subtree when a page is large.
   `find_similar_elements` shows where else a part occurs.
4. **Controls first.** For each pattern the user agrees to: a container
   (`ContainerBase<TParent, TSelf>`), a collection and its row
   (`CollectionObjectBase` / `ItemObjectBase`), in the config's `Controls` folder. Its first line
   is `scraped_header` for the page and the pattern's example node. Show the code, then
   `write_code`.
5. **Pages second.** One page object per recorded page the user wants, in the `Pages` folder:
   `HtmlPageObjectBase<TSelf>`, or `BlazorPageObjectBase<TSelf>` for a Blazor app that sets
   Brinell's busy signal (`data-busy` on `<body>`, as the Blazor sample does): it waits until the
   app is interactive. A Blazor app without it (a third party's) takes `HtmlPageObjectBase`. It uses the controls (`match_controls` says which occur on the
   page). Its first line is `scraped_header` for the page. `suggest_controls` gives a member per
   thing a test acts on or reads, with the control that fits (the `Brinell.Blazor.Controls` one in
   a Blazor project) and a locator: start from it, rename for what the user sees, drop what the
   page object does not need.
6. **Locators.** `suggest_controls` already follows this order, and says when it could not:
   `data-testid` (or the config's test-id attributes), the accessible name or label text, a
   stable `id`, a stable class. Never a generated class
   (`css-1x2y3z`, `jss12`, `sc-…`), a number in an id, or a position (`:nth-child`) unless
   nothing else is there, and then say so in the summary. Inside a frame, the frame is a
   `FrameScope`.
7. **The gate.** `check_code` builds the project with its analyzers. Fix every error it reports
   and call it again until it passes. The work is not done before that.

## Refreshing

`match_controls` says whether a page changed since its page object was made (the header's
hash). Record the page again, compare with `get_page_snapshot` of the old and new capture
(`session`, `number`), and change only what changed.

## Never

- Invent a page, control or element that is not in a recording.
- Write outside the project, or into `Recordings/`, `bin/` or `obj/` (`write_code` refuses).
- Put a customer's data in code: recordings are masked by the config's `## Masks`; generated
  names describe the element, not the record shown.
