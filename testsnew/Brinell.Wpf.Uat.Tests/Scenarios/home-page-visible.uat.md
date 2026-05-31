# UAT: WPF Home Page

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Wpf.App |
| Area | WPF |
| Target | WPF |
| Tags | wpf, smoke |

@wpf @smoke
## Scenario: Home page is visible

Given I am on the Home page
Then Welcome should be visible
And Description should be visible
