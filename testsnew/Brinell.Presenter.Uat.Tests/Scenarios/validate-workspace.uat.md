# UAT: Presenter Validates A UAT Suite

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Validation |
| Target | MAUI |
| Tags | smoke, presenter, validation |

@smoke @presenter @validation
## Scenario: Presenter validates the loaded suite

Given I am on the Presenter page
When I tap Validate
Then Status Summary should contain "Parse: ok"
And Status Summary should contain "Bind: ok"
