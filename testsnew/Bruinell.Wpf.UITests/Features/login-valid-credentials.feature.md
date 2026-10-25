# Feature: WPF Login

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinell.Samples.Wpf.App |
| Area | WPF |
| Target | WPF |
| Tags | wpf, login |

@wpf @login
## Scenario: Valid credentials navigate home

```gherkin
Given I am on the Login page
When I set Username to "demo"
And I set Password to "password"
And I tap Login
Then I should be on the Home page
And Welcome should be visible
```
