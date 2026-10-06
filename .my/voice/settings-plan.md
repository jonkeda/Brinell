# The cloud voice's settings in the Presenter

Status: **built**, steps 1-6 (2026-10-06): every question in section 5 settled as its leaning.
Date: 2026-10-05.

As built: the dogfood scenarios (`testsnew/Brinell.Presenter.Bdd.Tests/Features/cloud-voice-settings.feature.md`)
found that Brinell's MAUI driver set a **password field** through UI Automation's Value pattern,
which WinUI's password box takes without passing it to MAUI's `Entry.Text`: the step passed, the
app had nothing. A password field now takes its text through the app's `SetText` verb, and fails
saying so when the app does not declare it (`FlaUIMauiElement`); the key field declares it. The
Presenter's UAT fixture also sends the presented app to the back (`AppWindow = Background`), as
the Presenter itself is. Try with a real key is left to the owner.
Follows [plan.md](plan.md) (the cloud voice, built). Today `voice.secrets.json` is written by hand;
this lets the Presenter create, change, try and remove it.

## 1. Goal

In the Presenter's Settings panel (the gear), under **Narrator**, a **Cloud voice** part where a
person:

- sees whether a cloud voice is set up, and **from where** (an environment variable, a suite's file,
  or `%USERPROFILE%\.brinell\voice.secrets.json`);
- enters the endpoint, deployment, API version, key and default voice, and saves them;
- tries it: one sample line spoken by the service;
- removes it;
- then picks `azure:<voice>` in the existing Voice picker.

## 2. What holds today

| Fact | Where |
| --- | --- |
| The Settings panel is part of the page, opened from the gear; its Narrator part has the recordings folder, the Voice picker (`VoiceChoices`: "Default voice" and the Windows voices), Try, the beat and Speak aloud. | `srcnew/Brinell.Presenter/Views/PresenterPage.xaml` (from line 752), `ViewModels/PresenterShellViewModel.Recording.cs` |
| The Presenter's own settings are a JSON file it saves (`PresenterUserSettings`: `Voice`, `Beat`, ...). The key does not belong there: it is the narrator's file, read by every host (`dotnet test`, the IDEs, the Presenter). | `Services/PresenterUserSettings.cs` |
| `VoiceSecrets.Find(folder, read, home)` and `VoiceSecrets.Places(folder, home)` read the file and the environment; nothing writes the file. | `srcnew/Brinell.Narrator/VoiceSecrets.cs` |
| Try speaks through `IPresenterRecorder.TryVoiceAsync`, which uses `Speech.Find(voice)`: it already works with `azure:<voice>` once the file is there. | `Services/PresenterRecorder.cs` |
| The Presenter has a UAT suite of its own with page objects. | `testsnew/Brinell.Presenter.Bdd.Tests` (`Features/`, `PageObjects/`) |

## 3. Decisions

### S1. The Presenter writes the narrator's file, not a setting of its own

What it saves is `voice.secrets.json`, the same file `dotnet test` and the IDEs read (plan.md, V1).
So a voice set up in the Presenter is the voice of every recording on the machine.

### S2. Where it is saved

| Choice | When |
| --- | --- |
| **For me, on this machine**: `%USERPROFILE%\.brinell\voice.secrets.json` | the default; outside every repository |
| **For this workspace**: beside the open workspace's `uat.config.md` | offered only when `git check-ignore` says git ignores the file there; otherwise the choice is disabled and says which `.gitignore` line is missing (`**/voice.secrets.json`), with a button that adds it to that repository's `.gitignore` |

When an environment variable (`BRINELL_VOICE_KEY`, `BRINELL_VOICE_ENDPOINT`) is set, the part says
so: what it shows comes from there, and a saved file would not be used for those values.

### S3. The key is entered, never shown

The key is a password field. Once saved, the part shows **"key set"**, never the key or a part of it;
replacing it means typing a new one, and an empty key field on Save keeps the saved key. The key is
not in the Presenter's own settings, its log, its view model's state that is shown, or a test's
output. The file is written with access for the current user only (Windows ACL), whole, through a
temporary file and a move.

### S4. Try before Save

**Try** speaks "This is the voice of your video." with what is in the fields, before anything is
saved, through `AzureOpenAiSpeech` built from them (not from the file). Its result shows beside it:
"Spoken in azure:coral", or the service's own message (plan.md V6: refused key, no deployment,
busy, cannot be reached). Save stays possible either way: a person may set it up offline.

### S5. The Voice picker learns the cloud voices

With a cloud voice set up, `VoiceChoices` adds the cloud voices (`azure:alloy`, `azure:ash`,
`azure:ballad`, `azure:coral`, `azure:echo`, `azure:fable`, `azure:nova`, `azure:onyx`,
`azure:sage`, `azure:shimmer`, `azure:verse`), after the Windows ones; "Default voice" then means
the cloud voice's default (plan.md V3). The list refreshes on Save and Remove.

### S6. Remove

**Remove** deletes the file it shows (asking first, naming the file), never an environment
variable; the part then shows the next place that has settings, or "not set up".

### S7. The code that writes lives in the narrator

`VoiceSecrets` gains `Save(path)` (S3's write), `Delete(path)`, and `Source(folder, read, home)`:
which place gave each value (the environment, a file and its path). The Presenter only calls them,
so a later host (an IDE's settings page) writes the same way.

## 4. Steps

Each step one commit, with tests; the real service is never called by a test.

| # | Step | Files | Done when |
| --- | --- | --- | --- |
| 1 | **`VoiceSecrets.Save`, `Delete`, `Source`** (S3, S6, S7): the whole file through a temporary file; access for the current user only; an empty key keeps the saved one; `Source` names where each value comes from. | `srcnew/Brinell.Narrator/VoiceSecrets.cs` | Narrator tests: save and read back; the key kept on an empty key; the file's ACL has only the current user; `Source` with the environment, a suite's file, the user's file. |
| 2 | **Where it may be saved** (S2): `VoiceSecretsPlaces` answers whether git ignores the file in a folder (`git check-ignore`, and "no git" when there is none), and adds the `.gitignore` line on request. | `srcnew/Brinell.Narrator/VoiceSecretsPlaces.cs` | Tests in a temporary repository: ignored, not ignored, the line added makes it ignored, no git. |
| 3 | **The view model** (S1-S6): `PresenterShellViewModel.CloudVoice.cs`: the fields, where it is from, the place choice, Save, Try (from the fields), Remove, `VoiceChoices` with the cloud voices. The user's folder is injected, so tests never touch the real one. | `srcnew/Brinell.Presenter/ViewModels/` | Presenter tests: Save writes the chosen place; the key never in a bindable property; Try uses the fields (a fake recorder); Remove; the picker lists `azure:` voices after Save. |
| 4 | **The panel**: a "Cloud voice" part under Narrator: a state line ("Set up from %USERPROFILE%\.brinell\voice.secrets.json" / "from BRINELL_VOICE_KEY" / "Not set up"), the fields (the key an `IsPassword` entry), the place choice, Try, Save, Remove, the result line. AutomationIds on all of them. | `Views/PresenterPage.xaml` | Built; checked by the step 5 scenarios. |
| 5 | **Dogfood**: Presenter scenarios and page-object members for the part: set it up for me, it says where; save for a workspace whose repository does not ignore the file is refused and says which line; remove it. The Presenter under test gets a temporary user folder (an environment variable the Presenter reads in its tests only, e.g. `BRINELL_USERPROFILE`), so the real file is never touched. | `testsnew/Brinell.Presenter.Bdd.Tests/Features/`, `PageObjects/` | The scenarios pass; the developer's own `voice.secrets.json` is unchanged after the run. |
| 6 | **Docs**: the guide's Cloud Voice section gets "or in the Presenter: Settings, Narrator, Cloud voice"; the Presenter's guide its part. | `docs/guides/narrated-recordings.md`, `docs/platform-guides/presenter.md` | |

## 5. Open questions

| # | Question | Decided (the leaning) |
| --- | --- | --- |
| Q1 | A part of the existing Settings panel, or a page of its own? | A part of the panel, under Narrator: it is one more narrator setting, and the Voice picker beside it is what it changes. A page of its own if the panel grows too long. |
| Q2 | Show the last characters of the key, to tell two keys apart? | No: "key set" only (S3). The endpoint tells resources apart. |
| Q3 | The same page in Visual Studio's and VS Code's settings? | Later, on S7's code: an Options page in Visual Studio, a command in VS Code. |
| Q4 | Read the voices from the service instead of a fixed list (S5)? | When the service lists them; the fixed list until then (plan.md Q1). |
| Q5 | Store the key in the Windows Credential Manager instead of a file? | Not now: the file is what `dotnet test` and the build agents read. A later "store the key in Credential Manager" option would need the narrator to read it there too. |

## 6. Risks

- **The key leaks through the UI**: a password field, "key set" afterwards, no bindable property
  holding it once saved (step 3 tests), and nothing written to the Presenter's log.
- **A test overwrites the developer's real file**: the user folder is injected in unit tests and
  replaced in the dogfood scenarios (step 5), and step 5 checks the real file is unchanged.
- **Saved in a repository that does not ignore it**: S2 refuses that place until it is ignored.
