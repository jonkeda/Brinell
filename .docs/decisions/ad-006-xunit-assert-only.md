# AD-006: xUnit Assert Only

Use xUnit `Assert`. Do not add FluentAssertions.

**Ruled out:** a FluentAssertions dependency or `Should()` style assertions.

**Broken when:** a test project references FluentAssertions.
