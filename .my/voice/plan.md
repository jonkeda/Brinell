# A cloud voice for narrated recordings: Azure OpenAI text to speech

Status: **built, steps 1-6** (2026-10-05); step 7, a recording with the real service, is the owner's.
Date: 2026-10-05.

As built, where it differs from below:
- V2: the cloud voice is not declared with `[assembly: SpeechSynthesizer]`; `Speech.Find(voice,
  instructions, folder)` chooses it explicitly (V3's order), so the choice never depends on which
  assemblies are loaded. `Speech.Discover()` still finds the platform voices.
- V5: the cache is inside `AzureOpenAiSpeech` (its clips only), not in `Speech.ClipAsync`.
- Step 6: the Presenter's rehearsal speaks with the script's voice but not its instructions (its
  interface has no place for them; a recorded scene has them). The IDEs' Voice settings say
  `azure:<voice>`; they do not list the cloud voices.
- Tests: `testsnew/Brinell.Narrator.Tests/CloudVoiceTests.cs` (14), with a fake HTTP handler; none
  calls the service.
Adds a second voice next to the Windows one: Azure OpenAI's `gpt-4o-mini-tts`, called over HTTPS,
with its key in a file git never sees. Builds on
[narrated recordings](../../docs/guides/narrated-recordings.md) and
[AD-024](../../.docs/decisions/ad-024-a-recording-is-a-paced-gate-run.md).

## 1. Goal

A video script says `Voice | azure:coral` (or a suite's settings do), and its lines are spoken by
Azure OpenAI's text-to-speech deployment instead of a Windows voice. The key is set once per
developer, in a file that is ignored by git, or in an environment variable on a build agent. Nothing
else changes for the script, the pacing, the captions or the video.

Why: the Windows voices are flat, need Windows, and differ per machine. A cloud voice sounds the
same on every machine and every platform (an Android or web run too), and `gpt-4o-mini-tts` takes
instructions for tone ("calm, friendly, a product demo").

## 2. What holds today

| Fact | Where |
| --- | --- |
| A voice is an `ISpeechSynthesizer`: `IsAvailable`, and `SynthesizeAsync(text, voice, wavPath)` that writes a PCM WAV and returns its length. | `srcnew/Brinell.Narrator/Speech.cs` |
| Synthesizers are declared with `[assembly: SpeechSynthesizer(typeof(...))]`; `Speech.Discover()` loads the known platform packages and takes the **first available** one. Only `WindowsSpeech` (`Brinell.Narrator.Windows`) exists. | `Speech.cs`, `srcnew/Brinell.Narrator.Windows/WindowsSpeech.cs` |
| Without a synthesizer, a line's length is estimated from its words and it shows as a caption only. | `Speech.ClipAsync`, `Speech.Estimate` |
| The length of a clip comes from the WAV header (`Speech.WavDuration`, the `data` chunk's size). | `Speech.cs` |
| The voice is named by the script (`Voice` row), `BRINELL_VOICE`, or the IDEs' and Presenter's settings; a scene is out of date when its voice changed. | guide, `BddRecordingSession`, `BddNarrationPacer` |
| The Presenter makes `new WindowsSpeech()` itself in five places, not through `Speech.Discover`. | `srcnew/Brinell.Presenter/Services/PresenterRecorder.cs` |
| The repo already ignores local secrets: `*.env`, `**/TestSettings/**/*.secrets.json`, `**/TestSettings/secrets/*` (examples kept). | `.gitignore` lines 12-23 |

The service: `POST {endpoint}/openai/deployments/{deployment}/audio/speech?api-version={version}`
with the header `api-key: <key>` and a JSON body
`{ "model": "gpt-4o-mini-tts", "input": "<text>", "voice": "coral", "response_format": "wav", "instructions": "<tone>" }`;
the answer is the audio. For this plan: endpoint `https://jonk4-me1wfl8r-eastus2.openai.azure.com`,
deployment `gpt-4o-mini-tts`, api version `2025-03-01-preview`.

## 3. Decisions

### V1. The key lives outside the repository, or in a git-ignored file; never in a script

Looked up in this order; the first that has a key wins:

1. **Environment variables** `BRINELL_VOICE_KEY` (and `BRINELL_VOICE_ENDPOINT`,
   `BRINELL_VOICE_DEPLOYMENT`): for a build agent, from its secret store.
2. **`voice.secrets.json` in the suite's folder** (beside `uat.config.md`), then in each folder
   above it up to the repository root: for a key per repository. Git-ignored by a new rule
   `**/voice.secrets.json`; a committed `voice.secrets.example.json` shows the shape.
3. **`%USERPROFILE%\.brinell\voice.secrets.json`**: one key for every repository on the machine,
   never inside a working tree at all. The recommended place.

```json
{
  "endpoint": "https://jonk4-me1wfl8r-eastus2.openai.azure.com",
  "deployment": "gpt-4o-mini-tts",
  "apiVersion": "2025-03-01-preview",
  "key": "<the key>"
}
```

The key is never written anywhere else: not in a log, the Output pane, a timeline, a scene's hash,
an exception message or a test name. It goes only into the `api-key` header, never into a URL.
A check that the rule holds: `git check-ignore` on the file, in a test (step 1).

### V2. The cloud voice lives in `Brinell.Narrator`, not a package of its own

It needs only `HttpClient` and works on every platform, so none of AD-026's reasons for a project
of its own applies (no other target framework, no dependency users should not get). It is declared
with `[assembly: SpeechSynthesizer(typeof(AzureOpenAiSpeech))]` and is available when V1 finds an
endpoint, a deployment and a key.

### V3. A voice names its synthesizer

A voice is `azure:<name>` for the cloud voice (`azure:coral`, `azure:alloy`, ... as the service
lists them) and stays a plain name (`Microsoft Zira`) for Windows. `Speech.Discover(voice)` picks the
synthesizer that owns the name; with no voice named, the configured cloud voice comes first, then
Windows, then captions only. So a script that names a Windows voice records as before, and one
that names `azure:coral` fails clearly on a machine without a key (it does not fall back to a
different voice silently, since the scene would be out of date for the wrong reason).

### V4. Tone is part of the script

A script may add `| Instructions | calm and friendly, a product demo |` (and the settings a default).
It is sent as `instructions`, and it is part of a scene's hash, like the voice.

### V5. Clips are cached

The same line with the same voice and instructions is the same audio: clips are kept in
`%LOCALAPPDATA%\Brinell\speech-cache\<hash>.wav` (hash of synthesizer, voice, instructions, text),
so recording a scene again costs nothing and is not slowed by the network. A cache file is
written only when the whole answer arrived.

### V6. Failures say what to do

| Answer | Says |
| --- | --- |
| no key found | which places were looked in (V1), and that `voice.secrets.example.json` shows the shape |
| 401 / 403 | the key was refused: check it and the endpoint (the key is not shown) |
| 404 | no deployment by that name at that endpoint |
| 429 | the service is busy: retried after `Retry-After`, a few times, then fails naming it |
| a network error | the endpoint cannot be reached |

A failed line fails the recording; it never silently becomes a caption-only line.

## 4. Steps

Each step one commit, with tests; nothing calls the real service except step 7, by hand.

| # | Step | Files | Done when |
| --- | --- | --- | --- |
| 1 | **Settings and the key file** (V1): `VoiceSecrets` reads the three places in order; the example file; the `.gitignore` rule; the key never in `ToString` or an exception. | `srcnew/Brinell.Narrator/VoiceSecrets.cs`, `voice.secrets.example.json`, `.gitignore` | Unit tests: order, a missing file, a half-filled file, the key not in any message; a test that `git check-ignore` ignores `voice.secrets.json` in a suite folder. |
| 2 | **`AzureOpenAiSpeech`** (V2, V6): the request, the WAV written, its length. The service's WAV may carry a streaming header (data size `0xFFFFFFFF` or 0): the header is corrected from the file's length before `WavDuration` reads it. Retries on 429. | `srcnew/Brinell.Narrator/AzureOpenAiSpeech.cs` | Unit tests with a fake `HttpMessageHandler`: the URL and headers (key only in `api-key`), the body, a streaming-header WAV measured right, each error of V6. |
| 3 | **A voice names its synthesizer** (V3): `Speech.Discover(voice)`; `azure:` names; the order with no voice named. | `Speech.cs`, `BddRecordingSession.cs`, the narrator's callers | Unit tests: Windows name to Windows, `azure:coral` to the cloud voice, none to the configured one; a missing key with `azure:` fails naming V1's places. |
| 4 | **Instructions** (V4): the script's `Instructions` row, the settings' default, in the hash. | the video script parser, `BddNarrationPacer` | Parser and hash tests. |
| 5 | **The cache** (V5). | `Speech.ClipAsync` | Tests: a second clip of the same line makes no request; a different voice or instruction does. |
| 6 | **The Presenter and the IDEs** use `Speech.Discover`: the Presenter's five `new WindowsSpeech()`; the voice pickers list the cloud voices when a key is set. | `PresenterRecorder.cs`, the IDEs' voice settings | Presenter tests; the pickers checked by hand. |
| 7 | **By hand**: the owner records TOD.01's video script with `Voice | azure:coral` and their key; then the docs. | `docs/guides/narrated-recordings.md` (a "Cloud voice" section: the key file, the voices, cost), AD-024 (the key rule of V1, voices by synthesizer) | The video has the cloud voice; a second recording uses the cache. |

## 5. Open questions

| # | Question | Leaning |
| --- | --- | --- |
| Q1 | Which voice names does the deployment offer? | Read them from the service if it lists them; otherwise the documented set (alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, shimmer, verse). |
| Q2 | Fall back to Windows when the cloud voice fails mid-recording? | No (V3, V6): a different voice in one scene is worse than a clear failure. |
| Q3 | Microsoft Entra ID (no key) instead of a key? | Later: `DefaultAzureCredential` would need Azure.Identity, a dependency of its own. The key first, as asked. |
| Q4 | Cost: a long script spoken again and again. | The cache (V5) makes a re-recording free; the guide says what a minute costs. |
| Q5 | The playback during a rehearsal (Presenter) | It plays the cached WAV, as it plays a Windows clip now. |

## 6. Risks

- **The key leaks.** V1's three places, the ignore rule tested in step 1, and V1's "never written
  anywhere else" checked by tests. A key pasted into a script is not read as a key (the script has
  no key row), so it would at most be spoken aloud.
- **The service changes** (`api-version` is a preview). The version is a setting, defaulting to
  the one above.
- **Network in a test run.** A recording needs the network for lines not cached yet; a gate run
  without `BRINELL_RECORD` never calls the service.
