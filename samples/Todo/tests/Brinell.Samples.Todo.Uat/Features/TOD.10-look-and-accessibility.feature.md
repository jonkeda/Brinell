# Feature: TOD.10 Look and accessibility

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Screens |
| Target | MAUI, BLAZOR |
| Journey | TOD.10 |
| TestData | one-todo |
| Tags | maui, web, screens |

@TOD.10.1 @smoke @maui @web @screens
## Scenario: TOD.10.1 Every screen loads and its key controls exist

```gherkin
Given I am on the Todo List page
Then Add should exist
And Sync should exist
And Filter should exist
And Last Sync should exist
And Todos should have todo "Buy milk"
When I open the todo "Buy milk"
Then I should be on the Todo Detail page
And Title should exist
And Status should exist
And Due should exist
And Sync State should exist
And Edit should exist
And Delete should exist
When I tap Edit
Then I should be on the Todo Edit page
And Title should exist
And Notes should exist
And Has Due Date should exist
And Due Date should exist
And Status should exist
And Save should exist
And Cancel should exist
When I tap Cancel
Then I should be on the Todo Detail page
```
