# UAT: MAUI User Form Basic Input

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Maui.App |
| Area | User Form |
| Target | MAUI |
| Tags | smoke, maui, form |

@smoke @maui @form
## Scenario: User can enter basic profile information

Given I am on the User Form page
When I clear First Name
And I enter "Ada" into First Name
And I clear Last Name
And I enter "Lovelace" into Last Name
And I clear Email
And I enter "ada@example.com" into Email
And I check Terms
And I select "United States" from Country
Then First Name should contain "Ada"
And Terms should be checked
And Country should have selected "United States"
