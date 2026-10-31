# Feature: Presenter Runs A Sample UAT Scenario

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinet.Presenter |
| Area | Execution |
| Target | MAUI |
| Tags | smoke, presenter, execution |

@smoke @presenter @execution
## Scenario: Presenter runs the selected sample scenario

```gherkin
Given I am on the Presenter page
When I tap Run
Then Status Summary should contain "Passed: 1/1 scenarios"
```
