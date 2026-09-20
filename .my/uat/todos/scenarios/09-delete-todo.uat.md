# UAT: Deleting a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Delete a todo |
| Target | MAUI |
| Tags | todo, delete, custom-phrase |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Regression |
| Evidence | screenshot |
| Traces | TOD.06.1, TOD.06.2 |

<!--
Uses the custom phrases "I open the todo {title}" (OpenTodo), "I confirm the
dialog" (ConfirmDialog) and "I should not see {text}" (AssertTextAbsent). Delete
asks for confirmation (TOD.06.1); confirming returns to the list without the row
(TOD.06.2).
-->

@todo @delete @custom-phrase @automated
## Scenario: Confirming delete removes the todo from the list

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I enter "Cancel subscription" into Title
And I tap Save
Then I should be on the Todo List page
When I open the todo "Cancel subscription"
Then I should be on the Todo Detail page
When I tap Delete
And I confirm the dialog
Then I should be on the Todo List page
And I should not see "Cancel subscription"
```
