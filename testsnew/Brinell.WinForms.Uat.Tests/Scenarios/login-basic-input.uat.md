# UAT: WinForms Login

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.WinForms.App |
| Area | WinForms |
| Target | WINFORMS |
| Tags | winforms, login |

@winforms @login
## Scenario: Basic login input updates status

Given I am on the Login page
When I enter "Alice" into Username
And I enter "secret" into Password
And I check Remember
And I select "User" from Role
And I tap Login
Then Remember should be checked
And Status should contain "Logged in as Alice"
And Status should contain "(User)"
