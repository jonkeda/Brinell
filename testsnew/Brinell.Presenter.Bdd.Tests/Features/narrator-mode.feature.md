# Feature: Presenter Opens A Video Script In Narrator Mode

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Narrator |
| Target | MAUI |
| Tags | presenter, narrator |

Narrator mode, the third beside Present and Workspace, opens a video script (`*.video.md`) as a
tree: its scenes, each with a checkbox, the lines said and the steps run. The sample workspace has
one script, `Features/sample-tour.video.md`, which the mode opens when none is open yet. A scene
that has a current recording opens unchecked, so a scenario that counts checks them all first.

@presenter @narrator
## Scenario: Narrator mode shows the script's scenes and their lines

```gherkin
Given I am on the Presenter page
When I tap Reload
Then Status Summary should contain "Ready"
When I tap Narrator Mode
And I check All Scenes
Then Narrator Script should contain "sample-tour.video.md"
And Narrator Summary should contain "2 scenes, 2 checked"
And Narrator Tree should contain "A tour of the sample app"
And Narrator Tree should contain "1 A name is needed"
And Narrator Tree should contain "2 Filling in the form"
When I tap Scene 1 Toggle
Then Narrator Tree should contain "The sample greets you by name.  - intro"
And Narrator Tree should contain "Given I am on the Main page  - silent"
And Narrator Tree should contain "And I tap Greet  - Tap Greet without a name,"
```

@presenter @narrator
## Scenario: A scene's checkbox takes it out of the run

```gherkin
Given I am on the Presenter page
When I tap Narrator Mode
And I check All Scenes
Then Narrator Summary should contain "2 scenes, 2 checked"
And Scene 2 should be checked
When I uncheck Scene 2
Then Narrator Summary should contain "2 scenes, 1 checked"
And Scene 2 should be unchecked
When I check All Scenes
Then Narrator Summary should contain "2 scenes, 2 checked"
And Scene 2 should be checked
```

@presenter @narrator
## Scenario: Present mode comes back with the workspace tree

```gherkin
Given I am on the Presenter page
When I tap Narrator Mode
Then Narrator Script should contain "sample-tour.video.md"
When I tap Present Mode
Then Workspace Tree should contain "Features"
When I tap Narrator Mode
Then Narrator Script should contain "sample-tour.video.md"
```

@presenter @narrator @execution
## Scenario: Run rehearses the checked scenes, one after another

```gherkin
Given I am on the Presenter page
When I tap Narrator Mode
And I check All Scenes
Then Narrator Summary should contain "2 scenes, 2 checked"
When I tap Run
Then Status Summary should contain "Passed: 2/2 scenarios"
And Narrator Tree should contain "[x] ✓ 1 A name is needed"
And Narrator Tree should contain "[x] ✓ 2 Filling in the form"
```

@presenter @narrator @execution
## Scenario: An unchecked scene is not run

```gherkin
Given I am on the Presenter page
When I tap Narrator Mode
And I check All Scenes
And I uncheck Scene 2
And I tap Run
Then Status Summary should contain "Passed: 1/1 scenarios"
And Narrator Tree should contain "[x] ✓ 1 A name is needed"
And Scene 2 should be unchecked
```
