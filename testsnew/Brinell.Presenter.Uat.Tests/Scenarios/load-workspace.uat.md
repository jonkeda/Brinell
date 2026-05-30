# UAT: Presenter Loads A UAT Folder

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Workspace |
| Target | MAUI |
| Tags | smoke, presenter, load |

@smoke @presenter @load
## Scenario: Presenter shows the sample MAUI UAT workspace

Given I am on the Presenter page
Then Scenario List should contain "Greeting appears when a name is entered"
And Scenario List should contain "User can enter basic profile information"
And Workspace Summary should contain "App ok"
And Status Summary should contain "Ready"
