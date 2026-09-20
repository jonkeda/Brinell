# AD-010: MAUI Ahead Of Core, On Purpose

`Brinell.Maui` owns the interfaces its call model needs (`IMauiElement`,
`IMauiDriver`, `IMauiElementScope`, `IMauiPage`, `IMauiTestContext`) and no longer
implements Core's element, driver, scope and page interfaces. Core was not changed to
get there. The MAUI stack is where the model was designed and proven; the other stacks
keep Core's shapes until the proven shape moves down to Core, one stack at a time, as
its own project.

Until then, expect the MAUI types to differ from their Core counterparts. Do not add
adapters between the two: a bridge now would have to be undone in the move down.

**Broken when:** an adapter is added between the MAUI interfaces and Core's, or Core is
changed to match MAUI ahead of the deliberate move-down.
