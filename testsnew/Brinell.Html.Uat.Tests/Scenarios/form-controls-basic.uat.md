# UAT: HTML Form Controls

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Blazor.App |
| Area | Form Controls |
| Target | HTML |
| Tags | html, form |

@html @form
## Scenario: Form controls update visible state

Given I am on the Form Controls page
When I check Terms
And I uncheck Newsletter
And I select Germany from Country Select
Then Terms should be checked
And Newsletter should be unchecked
And Select Status should contain "Country: de"
