# Feature: Presenter Validates A UAT Suite

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinet.Presenter |
| Area | Validation |
| Target | MAUI |
| Tags | smoke, presenter, validation |

@smoke @presenter @validation
## Scenario: Presenter validates the loaded suite

```gherkin
Given I am on the Presenter page
When I tap Reload
Then Status Summary should contain "Parse: ok"
And Status Summary should contain "Bind: ok"
```
