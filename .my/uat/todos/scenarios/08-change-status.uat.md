# UAT: Cycling a todo's status

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Change status |
| Target | MAUI |
| Tags | todo, status, custom-phrase |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Regression |
| Evidence | none |
| Traces | TOD.05.1, TOD.05.5 |

<!--
Uses the custom phrases "I open the todo {title}" (OpenTodo), "I advance Status"
(AdvanceStatus) and "Status should read {value}" (AssertStatus). A new todo
starts Open; Next cycles Open -> In progress -> Done -> Open.
-->

@todo @status @custom-phrase @automated
## Scenario: Next moves Open to In progress on the detail page

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I enter "Water the plants" into Title
And I tap Save
Then I should be on the Todo List page
When I open the todo "Water the plants"
Then I should be on the Todo Detail page
And Status should read "Open"
When I advance Status
Then Status should read "In progress"
```
