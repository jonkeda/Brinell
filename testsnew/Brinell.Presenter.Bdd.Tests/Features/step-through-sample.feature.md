# Feature: Presenter Steps Through A Sample UAT Scenario

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Execution |
| Target | MAUI |
| Tags | smoke, presenter, execution |

A run starts paused before its first step, so what is about to happen can be said before it
happens. Next runs the highlighted step and pauses before the next; Run goes on to the end.

@smoke @presenter @execution
## Scenario: Presenter pauses before each step, then runs on

```gherkin
Given I am on the Presenter page
When I tap Reload
Then Status Summary should contain "Ready"
When I tap Next
Then Status Summary should contain "Paused before:"
When I tap Next
Then Step List should contain "✓"
And Status Summary should contain "Paused before:"
When I tap Run
Then Status Summary should contain "Passed:"
```
