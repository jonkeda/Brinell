# UAT: Reading a todo's detail

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Read a todo |
| Target | MAUI |
| Tags | smoke, todo, detail, custom-phrase |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Smoke |
| Evidence | none |
| Traces | TOD.03.1 |

<!--
Uses the custom phrase "I open the todo {title}" (OpenTodo). See ../journeys.md
"Custom phrases needed". Self-contained: it creates the todo it then reads.
-->

@smoke @todo @detail @custom-phrase @automated
## Scenario: Tapping a row opens its detail with the title shown

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I enter "Call the plumber" into Title
And I tap Save
Then I should be on the Todo List page
When I open the todo "Call the plumber"
Then I should be on the Todo Detail page
And Title should contain "Call the plumber"
And Sync State should be visible
```
