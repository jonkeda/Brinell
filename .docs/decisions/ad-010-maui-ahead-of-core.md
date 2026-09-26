# AD-010: MAUI And Html Ahead Of Core, On Purpose

`Brinell.Maui` owns the interfaces its call model needs (`IMauiElement`,
`IMauiDriver`, `IMauiElementScope`, `IMauiPage`, `IMauiTestContext`) and no longer
implements Core's element, driver, scope and page interfaces. Since 2026-09-26
`Brinell.Html` does the same, with its own set (`IHtmlElement`, `IHtmlDriver`,
`IHtmlElementScope`, `IHtmlPage`, `IHtmlTestContext`) and its own copy of the calls
layer. Core was not changed to get there, either time. The stacks where the model was
designed and proven keep their own shapes; the other stacks keep Core's until the proven
shape moves down to Core, one stack at a time, as its own project
(`.my/stale-readiness/move-down.md`).

Until then, expect the MAUI and Html types to differ from their Core counterparts, and
from each other where the platform differs (Html's `BusySignal`, for instance). Do not
add adapters between them and Core: a bridge now would have to be undone in the move
down.

**Broken when:** an adapter is added between the MAUI or Html interfaces and Core's, or
Core is changed to match either of them ahead of the deliberate move-down.
