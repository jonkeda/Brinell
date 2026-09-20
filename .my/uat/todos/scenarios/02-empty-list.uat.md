# UAT: The empty list stays actionable

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | See my todos |
| Target | MAUI |
| Tags | todo, list, empty |
| Mode | Automated |
| Requires | EmptyStore |
| Priority | Regression |
| Evidence | screenshot |
| Traces | TOD.01.3 |

@todo @list @empty @automated
## Scenario: With no todos, "Nothing to do" shows and Add stays reachable

```gherkin
Given I am on the Todo List page
Then State should contain "Nothing to do"
And Add should be visible
And Add should be enabled
```
