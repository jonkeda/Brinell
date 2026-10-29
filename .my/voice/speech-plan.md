# Azure Speech as a second cloud voice

Status: **plan**, not started. Date: 2026-10-29.

Follows [plan.md](plan.md) (the Azure OpenAI voice, built) and [settings-plan.md](settings-plan.md) (its
Presenter settings, built). This adds **Azure AI Speech**, the service with a named neural voice per
language and region, beside it: `Voice | speech:nl-NL-FennaNeural` in a video script, its endpoint
(URL) and key in the same settings.

## 1. Goal

A video script, the Presenter's Default voice or `BRUINET_VOICE` names `speech:<voice>`, and its lines
are spoken by Azure Speech. The endpoint and key are set once: in the Presenter (Settings, Narrator,
Azure Speech), in the settings file, or in environment variables. Pacing, captions, the clip cache
and the video are unchanged.

Why a second cloud voice: Azure OpenAI's voices are a short list that speaks every language in the
same few voices. Azure Speech has a named voice for each language and region (Dutch among them), the
same voice every time, billed per character with a free tier. The first stays; the script chooses.

## 2. The service (Microsoft Learn, text to speech REST reference, checked 2026-10-29)

| Part | What it says |
| --- | --- |
| Synthesis | `POST {endpoint}/cognitiveservices/v1`, body SSML (`<speak><voice name='nl-NL-FennaNeural'>…`), `Content-Type: application/ssml+xml`. Regional endpoint `https://{region}.tts.speech.microsoft.com/cognitiveservices/v1`; a resource with a custom subdomain, `https://{name}.cognitiveservices.azure.com`. |
| Key | Header `Ocp-Apim-Subscription-Key` is accepted on the call itself, and "works with all endpoint formats"; a bearer token (valid 10 minutes, scoped to the host that issued it) is the alternative. |
| Output | `X-Microsoft-OutputFormat: riff-24khz-16bit-mono-pcm` is a non-streaming WAV; 48 kHz also exists. `User-Agent` is required. |
| Voices | `GET …/tts/cognitiveservices/voices/list` with the same key: `ShortName`, `Locale`, `Gender`, `StyleList`, `VoiceType`, `Status`, `WordsPerMinute`. |
| Styles | `mstts:express-as style="friendly"` in the SSML, for the voices whose `StyleList` has it. |
| Errors | 400 bad request, 401 key or region wrong, 415 wrong content type, 429 too many, 502 and 503 server side. |
| Limit | Audio over 10 minutes is cut; a narrated line never nears it. |

Not yet known: which `…/cognitiveservices/v1` path the custom-subdomain host takes for synthesis
(the page shows `/cognitiveservices/v1` on the host and `/tts/cognitiveservices/…` for the voice list),
and whether the Foundry resource of plan.md (`jonk4-me1wfl8r-eastus2`, the same key) also serves Speech
at `https://jonk4-me1wfl8r-eastus2.cognitiveservices.azure.com`. Step 1 finds out.

## 3. What holds today

| Fact | Where |
| --- | --- |
| A voice is an `ISpeechSynthesizer` (`IsAvailable`, `SynthesizeAsync(text, voice, wavPath)` returning the length). | `srcnew/Bruinet.Narrator/Speech.cs` |
| `Speech.Find(voice, instructions, folder, read, home)` picks the synthesizer: the `azure:` prefix and a null voice go to `AzureOpenAiSpeech` when `VoiceSecrets` finds settings; a named `azure:` voice without settings throws `VoiceNotConfiguredException`; anything else is a platform voice. | `Speech.cs` |
| `AzureOpenAiSpeech` holds, besides its request: the clip cache (`%LOCALAPPDATA%\Bruinet\speech-cache`, written whole then moved), `FixHeader` (a streaming WAV's sizes), the retry on 429, the error messages. | `AzureOpenAiSpeech.cs` |
| `VoiceSecrets` (`voice.secrets.json`: `endpoint`, `deployment`, `apiVersion`, `key`, `voice`) is Azure OpenAI's; it reads the environment over the first file found up the suite's folders, then `%USERPROFILE%\.bruinet`; it has `Find`, `Places`, `Describe`, `Save`, `Delete`, and `VoiceSecretsPlaces` for the git-ignore rule. | `VoiceSecrets.cs` |
| The Presenter has a Cloud voice part (Azure OpenAI): fields, "key set", Try, Save, Remove, the workspace choice, and the Default voice picker with `azure:` voices. | `PresenterShellViewModel.CloudVoice.cs`, `PresenterPage.xaml` from line 874 |
| A video script has `Voice` and `Instructions` rows. | `BddVideoScriptParser.cs` |

## 4. Decisions

### A1. A voice names its service: `speech:`

`speech:nl-NL-FennaNeural` goes to Azure Speech; `azure:coral` stays Azure OpenAI's. A name with no prefix is a Windows voice, as before.
The language of the SSML (`xml:lang`) is the voice name's first two parts (`nl-NL`).

### A2. The settings join `voice.secrets.json`

One file, one git-ignore rule, one place to look. A `speech` object holds the Speech service's settings;
the existing top-level fields stay Azure OpenAI's, so a file that works today still works.

```json
{
  "endpoint": "https://<resource>.openai.azure.com", "deployment": "gpt-4o-mini-tts", "key": "<openai key>",
  "speech": {
    "endpoint": "https://<resource>.cognitiveservices.azure.com",
    "key": "<speech key>",
    "voice": "nl-NL-FennaNeural"
  }
}
```

Environment variables over the file, as for the first voice: `BRUINET_SPEECH_ENDPOINT`,
`BRUINET_SPEECH_KEY`, `BRUINET_SPEECH_VOICE`. The endpoint field takes a region name (`eastus2`, made
into the regional host), the resource URL, or a pasted URL with a path (the path dropped), as
`VoiceSecrets.ResourceOf` does for Azure OpenAI. The key rules of plan.md V1 hold unchanged: in no
log, URL, exception, hash or test name; only in the `Ocp-Apim-Subscription-Key` header.
Saving one service's settings keeps the other's.

### A3. The key header only

No token flow: no refresh, no host-scoped tokens, and the key works on every endpoint form.

### A4. The request

The narrator builds the SSML: the text XML-escaped (`&`, `<`, `>`, quotes), one `<voice>`; output
`riff-24khz-16bit-mono-pcm` (the Azure OpenAI voice's rate, so the video composer sees one shape);
`User-Agent: Bruinet.Narrator`. `FixHeader` runs anyway, as the length comes from the header.

### A5. Tone is a style

For a `speech:` voice the script's `Instructions` is a style name (`friendly`, `cheerful`), sent as
`mstts:express-as`. Free text is not understood by this service; the script check warns when the
voice's `StyleList` lacks the style, once the voices list is cached. Step 1 finds out what the service
does with a style the voice lacks.

### A6. What is shared is shared first

The cache, `FixHeader`, the retry and the error messages move out of `AzureOpenAiSpeech` into pieces
both voices use, before the new voice is written. The cache key includes the service, the voice, the
style and the text.

### A7. The voices come from the service

`AzureSpeechVoices.ListAsync` reads `voices/list`, kept per endpoint in the speech cache folder. The
Presenter lists the voices of one locale (the system's, changeable) and also takes a typed name; a
fixed list of hundreds would go stale.

### A8. With no voice named

Azure OpenAI stays first when it is set up, so nothing changes for anyone who has it; Azure Speech is
used when named, or when it is the only one set up; then Windows. A named service without settings
fails and names the places looked in; it never becomes another voice.

### A9. Failures say what to do

| Answer | Says |
| --- | --- |
| no key or endpoint | the places looked in, and that `voice.secrets.example.json` shows the shape |
| 401 | the key was refused, or belongs to another region (never shown) |
| 400 | the voice name or text was refused: check the name against the list |
| 429, 502, 503 | busy: retried after `Retry-After`, a few times, then fails naming it |
| network error | the endpoint cannot be reached |

## 5. Steps

Each step one commit, with tests; no test calls the real service.

| # | Step | Done when |
| --- | --- | --- |
| 1 | **By hand, before code** (the owner's key): `curl` the voices list and one SSML synthesis against the regional host and the resource host; try the resource of plan.md; try a style the voice lacks. Write the findings to `speech-results.md`. | A2's endpoint forms, A4's path and A5's behaviour are known |
| 2 | **Share the pieces** (A6) out of `AzureOpenAiSpeech`. | `CloudVoiceTests` pass unchanged |
| 3 | **Settings** (A2): the `speech` object, the variables, `Find`, `Places`, `Describe`, `Save`, `Delete` for it; the example file. | Tests: order, a half-filled section, saving one service keeps the other, the key in no message; the ignore rule covers the file as before |
| 4 | **`AzureSpeech`** (A1, A3, A4, A9) and `Speech.Find` for `speech:` and A8's order. | Fake `HttpMessageHandler` tests: the URL for a region, a resource URL and a pasted URL; the header; the SSML with `Tom & Jerry` escaped; the language from the name; each error; the cache serves a second line |
| 5 | **Styles** (A5): `Instructions` as `express-as`; the script check. | Parser, SSML and check tests |
| 6 | **The voices list** (A7). | Tests with a canned list: by locale, cached, a stale or broken cache refetched |
| 7 | **The Presenter**: an Azure Speech part under Narrator beside the Cloud voice part (state line, endpoint, key as a password field, default voice, locale and voice pickers, Try, Save, Remove, the workspace choice of settings-plan.md S2); the Default voice picker gains `speech:` voices. | Presenter tests and dogfood scenarios as in settings-plan.md step 5; the developer's real file unchanged after a run |
| 8 | **Docs and by hand**: the guide's Cloud voice section (Azure Speech: the settings, the voices, prices as the pricing page says then), AD-024 amended (voices by service); the owner records a script with `speech:`. | The video speaks the Speech voice; a second recording uses the cache |

## 6. Open questions

| # | Question | Leaning |
| --- | --- | --- |
| Q1 | The prefix: `speech:` or `azure-speech:`? | `speech:`: the voice names (`nl-NL-FennaNeural`) are plainly Azure's, and `azure:` is taken. |
| Q2 | The settings in the same file or `speech.secrets.json`? | The same file (A2): one ignore rule, one Presenter save path, and the first voice's file keeps working. |
| Q3 | Microsoft Entra ID instead of a key? | Later, as in plan.md Q3: it needs Azure.Identity and the `aad#` token form. |
| Q4 | HD voices and pronunciation (a lexicon, `sub` and `phoneme` in SSML) for product names? | Later: a voice name passes through already; pronunciation is its own step if a product name is said wrongly. |
| Q5 | 24 kHz or 48 kHz? | 24 kHz (A4); 48 kHz only if the video's audio is judged thin. |
| Q6 | Rate and pitch (`prosody`) in the script? | Not now: the beat paces the video, and a style gives the tone. |

## 7. Risks

- **The endpoint form is wrong for a custom-subdomain resource.** Step 1 finds the working form before any code; A2 accepts the forms found.
- **The shared pieces break the first voice.** Step 2 changes no behaviour and keeps `CloudVoiceTests` green.
- **Saving one service erases the other's settings.** Step 3 tests it both ways.
- **SSML breaks on a line with `&` or `<`.** The escape is tested with such a line.
- **The key leaks.** The rules of plan.md V1 and settings-plan.md S3, with the same tests on the new section.
- **A key from another region.** The 401 message says so, as the service's page does.
- **Cost of a long script.** The cache makes a re-recording free; the guide quotes the current prices.

## Sources

- [Text to speech REST API (Microsoft Learn)](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/rest-text-to-speech)
