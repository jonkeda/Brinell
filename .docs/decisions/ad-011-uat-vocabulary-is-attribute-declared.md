# AD-011: UAT Step Vocabulary Is Declared By Attributes On Core Interfaces

The built-in UAT step vocabulary lives as `[UatStep]` attributes on the
`Brinell.Core` control interfaces (`IClickableControlObject`, `IEditableTextControlObject`,
`IToggleControlObject`, `ISelectorControlObject`, `ITextControlObject`, `IElementObject`).
Each attribute maps a phrase to the control method it invokes. `Brinell.Uat` is the
execution engine: it discovers the vocabulary by reflection over those attributes and
projects it into every catalog it needs.

The three intrinsic page-graph verbs (`I am on the {page} page`,
`I should be on the {page} page`, `I should see {text}`) are the engine's own and stay
registered directly in the runtime and spec catalogs - they bind to no control method.

**Ruled out:** hand-written phrase tables. There is no second place that lists the
control phrases. The runtime catalog, the spec catalog and any preview all come from the
same `UatCatalogBuilder.DiscoverBindings` pass, so a phrase and the surface that runs it
cannot drift.

**Broken when:** a control phrase is added by editing a `catalog.Register(...)` call for a
control verb instead of adding a `[UatStep]` to the Core interface method; or a projection
(runtime, spec, presenter preview) re-lists control phrases by hand instead of calling the
discovery.
