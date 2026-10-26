# Feature: Presenter Records A Run On Video

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinet.Presenter |
| Area | Recording |
| Target | MAUI |
| Tags | presenter, recording |

Record in the run toolbar records each run on video while it is on (the narrator, AD-024). The
video is composed when the run ends, and Watch opens it. It needs ffmpeg on the machine that runs
Presenter. No video script narrates the sample scenario, so its video is silent.

@presenter @recording
## Scenario: Presenter records a run and offers to watch it

```gherkin
Given I am on the Presenter page
When I tap Reload
Then Status Summary should contain "Ready"
When I tap Record
Then Recording Status should contain "Recording is on"
When I tap Run
Then Status Summary should contain "Passed:"
And Recording Status should contain "Recorded "
And Recording Status should contain ".mp4"
And Recording Status should contain "no video script narrates this scenario"
And Watch Recording should exist
When I tap Record
Then Recording Status should equal "Recording is off."
```
