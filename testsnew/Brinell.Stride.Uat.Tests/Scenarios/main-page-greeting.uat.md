# UAT: STRIDE Main Page Greeting

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Stride.App |
| Area | STRIDE |
| Target | STRIDE |
| Tags | stride, greeting |

@stride @greeting
## Scenario: Greeting appears when a name is entered

Given I am on the Main page
When I set Name to "Alice"
And I tap Greet
Then Greeting should equal "Hello, Alice!"
