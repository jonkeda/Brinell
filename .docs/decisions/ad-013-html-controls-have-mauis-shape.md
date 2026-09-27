# AD-013: Html Controls Have MAUI's Shape

Html and Blazor control objects are made the way MAUI's are: a `.tpl.cs` template with `*Core`
methods, and a `.gen.cs` that `Brinell.Generator` writes from it (`tools/Scripts/CreateHtml.Bat`).
Html's `ControlBase` has MAUI's `Run*` helpers, name for name and signature for signature (R8),
so one generator serves both. The Html capability bases implement the same `Brinell.Core`
control interfaces as MAUI's, so a member a MAUI test or a UAT phrase uses has the same name,
parameters and meaning on Html. Every member is one synchronous call.

**Why:** hand-written Html controls approximated MAUI's surface by hand and drifted
(`AssertTextContaining` beside `AssertTextContains`, missing `Enter` and `AssertSelectedText`, so
UAT phrases failed on Html). The async members wrapped the synchronous calls and added nothing.
Evidence: [review-html-vs-maui.md at 7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/html/review-html-vs-maui.md), sections 1.3, 2 and 4.

**Ruled out:** hand-written public control members where a `*Core` method can generate them;
`Run*` helpers whose names or signatures differ from MAUI's; async control, page or container
members (`*Async` wrappers, async interfaces); a public member whose name differs from MAUI's for
the same behaviour.

**Broken when:** an Html or Blazor control with `*Core` methods is a plain `.cs`, or its
`.gen.cs` differs from what `CreateHtml.Bat` writes; the generator needs an Html-specific branch to
compile Html output; a `Task`-returning public member appears on an Html control, page or
container; a UAT phrase fails on Html with "available methods" for a method MAUI has.
