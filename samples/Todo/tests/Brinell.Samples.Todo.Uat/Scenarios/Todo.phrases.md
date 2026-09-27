# Phrases: Todo

## Phrase: the synced list has todos

```gherkin
Given I am on the Todo List page
And the server also has todos
  <table>
When I tap Sync
Then I should be on the Todo List page
```

## Phrase for each row: each todo shows its status

| Parameter | Type | Example          |
| --------- | ---- | ---------------- |
| title     | text | Post the letters |
| status    | text | In progress      |

```gherkin
Then the todo <title> should show status <status>
```
