# UAT: Filtering the list to Open

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | See my todos |
| Target | MAUI |
| Tags | todo, list, filter |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Regression |
| Evidence | none |
| Traces | TOD.01.4 |

@todo @list @filter @automated
## Scenario: Choosing the Open filter keeps the list on screen

```gherkin
Given I am on the Todo List page
When I select "Open" from Filter
Then Filter should have selected "Open"
And State should be visible
```
