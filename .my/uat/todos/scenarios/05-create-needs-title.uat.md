# UAT: A new todo needs a title

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Create a todo |
| Target | MAUI |
| Tags | smoke, todo, validation |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Smoke |
| Evidence | none |
| Traces | TOD.02.3 |

@smoke @todo @validation @automated @deterministic
## Scenario: Saving with a blank title is refused

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I clear Title
And I tap Save
Then Title Error should contain "Title is required"
And Title Error should be visible
```
