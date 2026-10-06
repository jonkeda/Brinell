---
mode: agent
description: Make Brinell page objects and controls from an app's recorded pages, controls first, until the project builds.
tools: ['brinell-bdd']
---

Follow the `scrape-pages` skill (`.github/skills/scrape-pages/SKILL.md`), in agent mode with the
`brinell-bdd` MCP server:

1. `list_scraped_projects`; pick the project the user means.
2. When pages are to be recorded: `start_recording`, let the user browse, `stop_recording`.
3. `list_controls`, `list_recorded_pages`, `detect_patterns`; propose the controls to make and
   the pages to cover, and wait for the user's choice.
4. Controls first, then pages: `get_page_snapshot` for the elements, `scraped_header` for each
   file's first line, show the code, then `write_code`.
5. `check_code` until it passes. Report what was made, and any locator that had to fall back to
   position.
