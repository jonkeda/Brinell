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
When I select "Auto" from Execution Mode
And I tap Run Selected
Then Status Summary should contain "Passed"
