# UAT: Editing a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Edit a todo |
| Target | MAUI |
| Tags | todo, edit, custom-phrase |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Regression |
| Evidence | none |
| Traces | TOD.04.1, TOD.04.2 |

<!-- Uses the custom phrase "I open the todo {title}" (OpenTodo). -->

@todo @edit @custom-phrase @automated
## Scenario: Changing the title updates the detail

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I enter "Draft report" into Title
And I tap Save
Then I should be on the Todo List page
When I open the todo "Draft report"
Then I should be on the Todo Detail page
When I tap Edit
Then I should be on the Todo Edit page
When I set Title to "Draft report v2"
And I tap Save
Then I should be on the Todo Detail page
And Title should contain "Draft report v2"
```
