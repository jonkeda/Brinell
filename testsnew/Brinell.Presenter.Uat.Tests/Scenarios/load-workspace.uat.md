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
When I tap Reload
Then Status Summary should contain "Ready"
Then Workspace Tree should contain "uat.config.md"
And Workspace Tree should contain "Scenarios"
And All Workspace Tree should contain "main-page-greeting.uat.md"
And All Workspace Tree should contain "user-form-basic-input.uat.md"
And Workspace Summary should contain "App ok"
