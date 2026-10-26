# Feature: TOD.11 Manage users, without Using

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinet.Samples.Todo.App |
| Journey | TOD.11 |
| TestData | team |

@maui
## Scenario: Users are not seen without Using

```gherkin
Given I am on the Todo List page
When I tap Users
Then I should be on the User List page
And the server should have users
  | Name |
  | ---- |
  | Eva  |
```
