# Scraper.Sites

What the Scraper makes of real sites: one public page per technology, explored, drafted and
verified. Not tests. The plan is [sites-plan.md](../.my/ScraperEx/sites-plan.md); how to explore
and draft is in [the guide](../docs/guides/scraper.md#explore-and-draft).

| Project | Site and page |
| --- | --- |
| `Bruinet.Html.Plain` | [the-internet `/tables`](https://the-internet.herokuapp.com/tables) |
| `Bruinet.Html.React` | [DemoQA `/tabs`](https://demoqa.com/tabs) |
| `Bruinet.Html.Vue2` | [Element UI tabs](https://element.eleme.io/#/en-US/component/tabs) |
| `Bruinet.Html.Blazor` | [MudBlazor tabs](https://mudblazor.com/components/tabs) |

Explore, draft and verify one (from this folder):

```powershell
$bruinet = '..\tools\Scripts\bruinet.ps1'
& $bruinet scrape explore --project Bruinet.Html.Plain --solution . --headless
& $bruinet scrape draft --project Bruinet.Html.Plain --solution .
& $bruinet scrape verify --project Bruinet.Html.Plain --solution . --headless
```

The sites' recordings stay out of git (their content is not ours); the drafted page objects are
committed.
