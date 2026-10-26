# Feature: BLAZOR Login

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinet.Samples.Blazor.App |
| Area | Login |
| Target | BLAZOR |
| Tags | blazor, login |

@blazor @login
## Scenario: Login rejects invalid credentials

```gherkin
Given I am on the Login page
When I set Email to "test@example.com"
And I set Password to "wrong-password"
And I tap Login
Then Error should contain "Invalid email or password"
```
