# Feature: Presenter Sets Up The Cloud Voice

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinet.Presenter |
| Area | Recording |
| Target | MAUI |
| Tags | presenter, narrator, settings |

Settings, Narrator, Cloud voice writes the narrator's `voice.secrets.json`, which every recording
on the machine reads. The suite points the user's folder at a folder of its own
(`BRUINET_USERPROFILE`), so these scenarios never touch the developer's real file. Nothing here
calls the service: Try is left to the developer, with a real key.

@presenter @narrator @settings
## Scenario: A cloud voice set up for this machine is saved, then removed

```gherkin
Given I am on the Presenter page
When I tap Settings
Then Cloud Voice State should equal "Not set up."
And Cloud Save Path should contain ".bruinet"
When I enter "https://voice.openai.azure.com" into Cloud Endpoint
And I enter "not-a-real-key" into Cloud Key
And I select "coral" from Cloud Default Voice
And I tap Save Cloud Voice
Then Cloud Voice Status should contain "Saved to"
And Cloud Voice State should contain "Set up from"
And Cloud Voice State should contain "voice.secrets.json"
When I tap Remove Cloud Voice
Then Cloud Voice Status should contain "Click again to delete"
When I tap Remove Cloud Voice
Then Cloud Voice State should equal "Not set up."
When I tap Settings
Then Status Summary should contain "Ready"
```

@presenter @narrator @settings
## Scenario: The workspace may hold the file because its repository ignores it

```gherkin
Given I am on the Presenter page
When I tap Settings
Then Cloud Workspace Note should equal "Git ignores the file beside this workspace."
When I tap Settings
Then Status Summary should contain "Ready"
```
