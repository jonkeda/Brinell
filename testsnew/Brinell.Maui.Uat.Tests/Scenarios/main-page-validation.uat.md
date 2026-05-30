# UAT: MAUI Main Page Greeting Validation

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Maui.App |
| Area | Main Page |
| Target | MAUI |
| Tags | smoke, maui, validation |

@smoke @maui @validation
## Scenario: Empty name shows validation message

Given I am on the Main page
When I clear Name
And I tap Greet
Then Greeting should contain "Please enter your name"
