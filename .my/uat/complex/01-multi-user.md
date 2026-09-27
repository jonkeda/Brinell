# 01 — Multi-user UAT

Status: **trimmed 2026-09-27** to what is still open. Most of this note is built: a scenario can
run several apps, a step names its user (`Daan taps Save`, AD-015), apps of different heads meet
in a `mixed` suite (AD-014), and a second user can still be simulated through the backend
(`MockApiServer`). How to write those is in
[docs/guides/uat-phrases-and-flows.md](../../../docs/guides/uat-phrases-and-flows.md) ("Several
Users And Apps") and the [apps contract](../../../.docs/contracts/uat-apps-host.md). The full
note is [at 7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/uat/complex/01-multi-user.md).

## Open: identity switching (per-user isolation)

Proving that user B cannot see user A's data needs users that are accounts, not clients of one
list. The Todo backend has one list, so "Daan" and "Eva" are two clients of it (the multi-app
work's limit D10). What it would take:

- a backend with per-user data, or a test seam in the app's sign-in;
- domain phrases for signing in and out, in the app's phrase class, not built-ins:

  ```gherkin
  Given I am signed in as "alice"
  And I create the todo "Alice private"
  When I sign out
  And I sign in as "bob"
  Then I should not see "Alice private"
  ```

- the rule itself (who may see what) covered by a unit test, with UAT checking only what the
  screen shows.
