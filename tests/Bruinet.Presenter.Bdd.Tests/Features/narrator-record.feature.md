# Feature: Presenter Records A Video Script Scene By Scene

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinet.Presenter |
| Area | Narrator |
| Target | MAUI |
| Tags | presenter, narrator, recording |

In Narrator mode, Record records the checked scenes, each into its own kept recording; a scene that
passes is unchecked. When every scene is recorded the video is stitched, and Stitch does it again
on demand. A scene recorded again replaces that scene only. It needs ffmpeg on the machine that
runs Presenter.

@presenter @narrator @recording
## Scenario: Record records the checked scenes and stitches the video

```gherkin
Given I am on the Presenter page
When I tap Narrator Mode
And I check All Scenes
Then Narrator Summary should contain "2 scenes, 2 checked"
When I tap Record
Then Recording Status should contain "Stitched sample-tour-"
And Recording Status should contain ".mp4"
And Status Summary should contain "Passed: 2/2 scenarios"
And Narrator Summary should contain "2 scenes, 0 checked, 2 recorded"
And Narrator Tree should contain "1 A name is needed  (recorded"
And Narrator Tree should contain "2 Filling in the form  (recorded"
And Watch Recording should exist
And Scene 1 Watch should exist
```

@presenter @narrator @recording
## Scenario: One scene is recorded again and the video stitched again

```gherkin
Given I am on the Presenter page
When I tap Narrator Mode
Then Narrator Summary should contain "2 recorded"
When I check Scene 1
Then Narrator Summary should contain "2 scenes, 1 checked"
When I tap Record
Then Status Summary should contain "Passed: 1/1 scenarios"
And Recording Status should contain "Stitched sample-tour-"
And Narrator Summary should contain "2 scenes, 0 checked, 2 recorded"
When I tap Stitch
Then Recording Status should contain "Stitched sample-tour-"
```
