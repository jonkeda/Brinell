# UAT: TOD.07 Sync between apps of several heads

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo |
| Area | Sync |
| Target | MIXED |
| Journey | TOD.07 |
| Tags | mixed, sync, multi-app |
| Apps | apps.md: Daan and Gus on Windows, Finn in a browser. Each step runs on the app of the user it names. |

@TOD.07.11 @mixed @sync
## Scenario: TOD.07.11 A todo Daan creates on Windows reaches Finn's browser

```gherkin
Given Daan is on the Todo List page
And Finn is on the Todo List page
When Daan taps Add
Then Daan should be on the Todo Edit page
When Daan sets Title to "Pack for the trip"
And Daan taps Save
Then Daan should be on the Todo List page
When Daan taps Sync
Then the server should have received the todo "Pack for the trip" from Daan
And Finn should be on the Todo List page
And Todos should not have todo "Pack for the trip"
When Finn taps Sync
Then Todos should have todo "Pack for the trip"
```

@TOD.07.12 @mixed @sync
## Scenario: TOD.07.12 A todo Gus creates reaches Daan's PC and Finn's browser

```gherkin
Given Daan is on the Todo List page
And Gus is on the Todo List page
And Finn is on the Todo List page
When Gus taps Add
Then Gus should be on the Todo Edit page
When Gus sets Title to "Water the plants"
And Gus taps Save
Then Gus should be on the Todo List page
When Gus taps Sync
Then the server should have received the todo "Water the plants" from Gus
When Daan taps Sync
Then Todos should have todo "Water the plants"
When Finn taps Sync
Then Todos should have todo "Water the plants"
```

@TOD.07.14 @mixed @sync
## Scenario: TOD.07.14 Finn's browser, offline, gets Daan's todo once it is back online

```gherkin
Given Daan is on the Todo List page
And Finn is on the Todo List page
And Finn is offline
Then Sync should be disabled
When Daan taps Add
Then Daan should be on the Todo Edit page
When Daan sets Title to "Renew the insurance"
And Daan taps Save
Then Daan should be on the Todo List page
When Daan taps Sync
Then the server should have received the todo "Renew the insurance" from Daan
When Finn is back online
Then Sync should be enabled
When Finn taps Sync
Then Todos should have todo "Renew the insurance"
```

@TOD.07.15 @mixed @sync @android
## Scenario: TOD.07.15 A todo Eva creates on her phone reaches Daan's PC

```gherkin
Given Eva is on the Todo List page
And Daan is on the Todo List page
When Eva taps Add
Then Eva should be on the Todo Edit page
When Eva sets Title to "Call the plumber"
And Eva taps Save
Then Eva should be on the Todo List page
When Eva taps Sync
Then the server should have received the todo "Call the plumber" from Eva
When Daan taps Sync
Then Todos should have todo "Call the plumber"
```
