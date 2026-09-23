# UAT: Presenter Edits The Workspace Config

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Config |
| Target | MAUI |
| Tags | smoke, presenter, config |

@smoke @presenter @config
## Scenario: Config tab shows what the config derives to

Given I am on the Presenter page
When I tap Config
Then Config State should contain "uat.config.md"
And Config Derived Target should contain "MAUI"
And Config Derived Pages should contain "Brinell.Maui.UITests.dll"
And Config Derived App should contain "configured override"
When I tap Tree

@smoke @presenter @config
## Scenario: Editing the fixture marks the config unsaved and revert takes it back

Given I am on the Presenter page
When I tap Config
And I clear Config Fixture
And I enter "ShellFixture" into Config Fixture
Then Config State should contain "unsaved"
When I tap Config Revert
Then Config Fixture should contain "MauiFixture"
When I tap Tree
