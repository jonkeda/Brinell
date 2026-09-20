# AD-002: Page Objects Own Structure

Tests should describe user intent. Page objects expose meaningful operations and
controls; they do not leak locator plumbing into test methods.

**Ruled out:** `Locator` construction, `FindElement` calls, or driver access in a
test method.

**Broken when:** a test method builds a locator or reaches the driver instead of
calling a page-object member.
