# UAT: Presenter Runs A Sample UAT Scenario

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Execution |
| Target | MAUI |
| Tags | smoke, presenter, execution |

@smoke @presenter @execution
## Scenario: Presenter runs the selected sample scenario

Given I am on the Presenter page
When I tap Run
Then Status Summary should contain "Passed"
