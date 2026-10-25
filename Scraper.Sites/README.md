# Scraper.Sites

What the Scraper makes of real sites: one public page per technology, explored, drafted and
verified. Not tests. The plan is [sites-plan.md](../.my/ScraperEx/sites-plan.md); how to explore
and draft is in [the guide](../docs/guides/scraper.md#explore-and-draft).

| Project | Site and page |
| --- | --- |
| `Bruinell.Html.Plain` | [the-internet `/tables`](https://the-internet.herokuapp.com/tables) |
| `Bruinell.Html.React` | [DemoQA `/tabs`](https://demoqa.com/tabs) |
| `Bruinell.Html.Vue2` | [Element UI tabs](https://element.eleme.io/#/en-US/component/tabs) |
| `Bruinell.Html.Blazor` | [MudBlazor tabs](https://mudblazor.com/components/tabs) |

Explore, draft and verify one (from this folder):

```powershell
$bruinell = '..\tools\Scripts\bruinell.ps1'
& $bruinell scrape explore --project Bruinell.Html.Plain --solution . --headless
& $bruinell scrape draft --project Bruinell.Html.Plain --solution .
& $bruinell scrape verify --project Bruinell.Html.Plain --solution . --headless
```

The sites' recordings stay out of git (their content is not ours); the drafted page objects are
committed.
