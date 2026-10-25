# Feature: BLAZOR Counter

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinell.Samples.Blazor.App |
| Area | Counter |
| Target | BLAZOR |
| Tags | blazor, counter |

@blazor @counter
## Scenario: Counter increments from the button

```gherkin
Given I am on the Counter page
Then Count should equal "Current count: 0"
When I tap Increment
Then Count should equal "Current count: 1"
```
