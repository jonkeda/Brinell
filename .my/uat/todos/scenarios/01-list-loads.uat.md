# UAT: The todo list loads

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | See my todos |
| Target | MAUI |
| Tags | smoke, todo, list |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Smoke |
| Evidence | none |
| Traces | TOD.01.1 |

@smoke @todo @list @automated @deterministic
## Scenario: Opening the app shows the list

```gherkin
Given I am on the Todo List page
Then State should be visible
And Add should be visible
And Add should be enabled
```
