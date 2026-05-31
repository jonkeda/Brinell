# UAT: HTML Counter

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Blazor.App |
| Area | Counter |
| Target | HTML |
| Tags | html, counter |

@html @counter
## Scenario: Counter increments from the button

Given I am on the Counter page
Then Count should equal "Current count: 0"
When I tap Increment
Then Count should equal "Current count: 1"
