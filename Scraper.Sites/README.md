# Scraper.Sites

What the Scraper makes of real sites: one public page per technology, explored, drafted and
verified. Not tests. The plan is [sites-plan.md](../.my/ScraperEx/sites-plan.md); how to explore
and draft is in [the guide](../docs/guides/scraper.md#explore-and-draft).

| Project | Site and page |
| --- | --- |
| `Brinell.Html.Plain` | [the-internet `/tables`](https://the-internet.herokuapp.com/tables) |
| `Brinell.Html.React` | [DemoQA `/tabs`](https://demoqa.com/tabs) |
| `Brinell.Html.Vue2` | [Element UI tabs](https://element.eleme.io/#/en-US/component/tabs) |
| `Brinell.Html.Blazor` | [MudBlazor tabs](https://mudblazor.com/components/tabs) |

Explore, draft and verify one (from this folder):

```powershell
$brinell = '..\tools\Scripts\brinell.ps1'
& $brinell scrape explore --project Brinell.Html.Plain --solution . --headless
& $brinell scrape draft --project Brinell.Html.Plain --solution .
& $brinell scrape verify --project Brinell.Html.Plain --solution . --headless
```

The sites' recordings stay out of git (their content is not ours); the drafted page objects are
committed.
