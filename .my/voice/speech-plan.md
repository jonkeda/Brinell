# Azure Speech as a second cloud voice

Status: **plan, decided** (the owner, 2026-10-29, section 6); not started. Date: 2026-10-29.

Follows [plan.md](plan.md) (the Azure OpenAI voice, built) and [settings-plan.md](settings-plan.md) (its
Presenter settings, built). This adds **Azure AI Speech**, the service with a named neural voice per
language and region, beside it: `Voice | azure-speech-service:en-GB-Emily:MAI-Voice-2.1` in a video script, its
endpoint (URL) and key in the same settings. The first voice is **Emily**, one of Microsoft's MAI
voices (preview), picked in the Foundry voice gallery.

## 1. Goal

A video script, the Presenter's Default voice or `BRUINET_VOICE` names `azure-speech-service:<voice>`, such as
`azure-speech-service:en-GB-Emily:MAI-Voice-2.1`, and its lines are spoken by Azure Speech. The endpoint and key are set once: in the Presenter (Settings, Narrator,
Azure Speech), in the settings file, or in environment variables. Pacing, captions, the clip cache
and the video are unchanged.

Why a second cloud voice: Azure OpenAI's voices are a short list that speaks every language in the
same few voices. Azure Speech has a named voice for each language and region (Dutch among them), the
same voice every time, billed per character with a free tier, and the MAI voices are expressive and
built for long narration, which is what a video voice-over is. The first stays; the script chooses.

## 2. The service (Microsoft Learn, text to speech REST reference, checked 2026-10-29)

| Part | What it says |
| --- | --- |
| Synthesis | `POST {endpoint}/cognitiveservices/v1`, body SSML (`<speak><voice name='nl-NL-FennaNeural'>…`), `Content-Type: application/ssml+xml`. Regional endpoint `https://{region}.tts.speech.microsoft.com/cognitiveservices/v1`; a resource with a custom subdomain, `https://{name}.cognitiveservices.azure.com`. |
| Key | Header `Ocp-Apim-Subscription-Key` is accepted on the call itself, and "works with all endpoint formats"; a bearer token (valid 10 minutes, scoped to the host that issued it) is the alternative. |
| Output | `X-Microsoft-OutputFormat: riff-24khz-16bit-mono-pcm` is a non-streaming WAV; 48 kHz also exists. `User-Agent` is required. |
| Voices | `GET …/tts/cognitiveservices/voices/list` with the same key: `ShortName`, `Locale`, `Gender`, `StyleList`, `VoiceType`, `Status`, `WordsPerMinute`. |
| Styles | `mstts:express-as style="friendly"` in the SSML, for the voices whose `StyleList` has it. |
| MAI voices | The same API, key and endpoint forms. The SSML name is the voice ID, a colon and the model: `en-GB-Emily:MAI-Voice-2.1` (`MAI-Voice-2.1-Flash` is the low-latency model; 2.1 is for long-form narration and fidelity). 23 languages; Emily and Harry (`en-GB-Harry`) are British English. Served from 14 regions, `eastus2` among them. Public preview: no service-level agreement. |
| MAI styles | Per voice, from the docs' table. Emily: `agent`, `angry`, `audiobook`, `confused`, `customer_call_center`, `disgusted`, `educational`, `embarrassed`, `excited`, `fearful`, `happy`, `jealous`, `joyful`, `narrator`, `neutral`, `sad`, `surprised`. Harry: `agent`, `angry`, `audiobook`, `customer_call_center`, `disgusted`, `educational`, `fearful`, `joyful`, `narrator`, `neutral`, `sad`, `surprised`. Both have `narrator`, `educational` and `audiobook`. The page's own sample uses `style="happiness"`, which neither table lists. |
| Errors | 400 bad request, 401 key or region wrong, 415 wrong content type, 429 too many, 502 and 503 server side. |
| Limit | Audio over 10 minutes is cut; a narrated line never nears it. |

Not yet known: whether the MAI voices answer in a WAV (`riff-24khz-16bit-mono-pcm`; the page's sample asks
for MP3), which style names the service takes, whether `voices/list` lists them and under which names,
which `…/cognitiveservices/v1` path the custom-subdomain host takes for synthesis
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

### A1. A voice names its service: `azure-speech-service:`

`azure-speech-service:en-GB-Emily:MAI-Voice-2.1` goes to Azure Speech; `azure:coral` stays Azure OpenAI's. A name with
no prefix is a Windows voice, as before. Only the first `azure-speech-service:` is cut; the rest, including the
`:MAI-Voice-2.1` model suffix, goes to the service whole, so a neural voice (`nl-NL-FennaNeural`) and a
MAI voice are named the same way. The language of the SSML (`xml:lang`) is the voice name's first two
parts (`en-GB`). The prefix does not clash with `azure:`: the character after `azure` differs, and each
voice's `Owns` test is exact. The settings keep the short name: the `speech` object and `BRUINET_SPEECH_*`.

### A2. The settings join `voice.secrets.json`

One file, one git-ignore rule, one place to look. A `speech` object holds the Speech service's settings;
the existing top-level fields stay Azure OpenAI's, so a file that works today still works.

```json
{
  "endpoint": "https://<resource>.openai.azure.com", "deployment": "gpt-4o-mini-tts", "key": "<openai key>",
  "speech": {
    "endpoint": "https://<resource>.cognitiveservices.azure.com",
    "key": "<speech key>",
    "voice": "en-GB-Emily:MAI-Voice-2.1",
    "sampleRate": 24000
  }
}
```

Environment variables over the file, as for the first voice: `BRUINET_SPEECH_ENDPOINT`,
`BRUINET_SPEECH_KEY`, `BRUINET_SPEECH_VOICE`, `BRUINET_SPEECH_SAMPLE_RATE`. `sampleRate` is `24000` (the
default) or `48000`; anything else is refused with a message naming both. The endpoint field takes a region name (`eastus2`, made
into the regional host), the resource URL, or a pasted URL with a path (the path dropped), as
`VoiceSecrets.ResourceOf` does for Azure OpenAI. The key rules of plan.md V1 hold unchanged: in no
log, URL, exception, hash or test name; only in the `Ocp-Apim-Subscription-Key` header.
Saving one service's settings keeps the other's.

### A3. The key header only

No token flow: no refresh, no host-scoped tokens, and the key works on every endpoint form.

### A4. The request

The narrator builds the SSML: the text XML-escaped (`&`, `<`, `>`, quotes), one `<voice>`; output
`riff-24khz-16bit-mono-pcm`, or `riff-48khz-16bit-mono-pcm` when `sampleRate` says 48000 (24 kHz is the
Azure OpenAI voice's rate, so the default keeps one shape for the composer);
`User-Agent: Bruinet.Narrator`. `FixHeader` runs anyway, as the length comes from the header. If a MAI
voice will not answer in a WAV (step 1), the request asks for `raw-24khz-16bit-mono-pcm` (or 48) and the narrator
writes the 44-byte WAV header itself: no MP3 decoder, which the narrator does not have.

### A5. Tone is a style

For a `azure-speech-service:` voice the script's `Instructions` is a style name, sent as `mstts:express-as`: for a
video voice-over `narrator`, `educational` or `audiobook`, which Emily and Harry both have. Free text is not understood by this service; the script check warns when the
voice's `StyleList` lacks the style, once the voices list is cached. Step 1 finds out what the service
does with a style the voice lacks.

### A6. What is shared is shared first

The cache, `FixHeader`, the retry and the error messages move out of `AzureOpenAiSpeech` into pieces
both voices use, before the new voice is written. The cache key includes the service, the voice, the
style, the sample rate and the text.

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

### A10. MAI-Voice-2.1, not Flash, for videos

The model is part of the name, so a script picks it. Videos are rendered ahead, so latency does not
matter and the long-form model's fidelity does; `-Flash` is for a voice that answers a person live.
Emily is the first default (she is listed first in the gallery); Harry is a name away.

## 5. Steps

Each step one commit, with tests; no test calls the real service.

| # | Step | Done when |
| --- | --- | --- |
| 1 | **By hand, before code** (the owner's key, below): Emily in a WAV against the regional host, the same with raw PCM if the WAV fails, the resource host of plan.md's Foundry resource, the voices list, and a style the voice lacks (`whispering` on Harry). Write the findings to `speech-results.md`. | A2's endpoint forms, A4's output, A5's styles and A7's list are known |
| 2 | **Share the pieces** (A6) out of `AzureOpenAiSpeech`. | `CloudVoiceTests` pass unchanged |
| 3 | **Settings** (A2): the `speech` object (with `sampleRate`), the variables, `Find`, `Places`, `Describe`, `Save`, `Delete` for it; the example file. | Tests: order, a half-filled section, a sample rate other than 24000 or 48000 refused, saving one service keeps the other, the key in no message; the ignore rule covers the file as before |
| 4 | **`AzureSpeech`** (A1, A3, A4, A9) and `Speech.Find` for `azure-speech-service:` and A8's order. | Fake `HttpMessageHandler` tests: the URL for a region, a resource URL and a pasted URL; the header; the SSML with `Tom & Jerry` escaped; the language from the name; each error; the 24 and 48 kHz formats asked for by the setting; the cache serves a second line, and a different rate is a different clip; the video composer takes a 48 kHz clip |
| 5 | **Styles** (A5): `Instructions` as `express-as`; the script check. | Parser, SSML and check tests |
| 6 | **The voices list** (A7). | Tests with a canned list: by locale, cached, a stale or broken cache refetched |
| 7 | **The Presenter**: an Azure Speech part under Narrator beside the Cloud voice part (state line, endpoint, key as a password field, default voice, locale and voice pickers, a 24 or 48 kHz choice, Try, Save, Remove, the workspace choice of settings-plan.md S2); the Default voice picker gains `azure-speech-service:` voices. | Presenter tests and dogfood scenarios as in settings-plan.md step 5; the developer's real file unchanged after a run |
| 8 | **Docs and by hand**: the guide's Cloud voice section (Azure Speech: the settings, the MAI voices and their styles, prices as the pricing page says then), AD-024 amended (voices by service); the owner records a script with `azure-speech-service:en-GB-Emily:MAI-Voice-2.1`. | The video speaks the Speech voice; a second recording uses the cache |

### Step 1, by hand (PowerShell 7)

The key stays in the owner's shell; nothing here is saved. Set `$key` to the Speech resource's key (the
Keys and Endpoint page of the Foundry resource) and `$region` to its region (`eastus2`).

```powershell
@'
<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xmlns:mstts="http://www.w3.org/2001/mstts" xml:lang="en-GB">
  <voice name="en-GB-Emily:MAI-Voice-2.1">
    <mstts:express-as style="narrator">This is Emily, the voice of the video.</mstts:express-as>
  </voice>
</speak>
'@ | Set-Content emily.ssml

curl.exe -sS -D - -o emily.wav -X POST "https://$region.tts.speech.microsoft.com/cognitiveservices/v1" `
  -H "Ocp-Apim-Subscription-Key: $key" -H "Content-Type: application/ssml+xml" `
  -H "X-Microsoft-OutputFormat: riff-24khz-16bit-mono-pcm" -H "User-Agent: Bruinet.Narrator" --data-binary "@emily.ssml"
```

Then the same with `riff-48khz-16bit-mono-pcm` (does Emily answer at 48 kHz?), with `raw-24khz-16bit-mono-pcm`
and `-o emily.raw` (if the WAV answer is not a 200 or does not play), with the host `https://<resource>.cognitiveservices.azure.com` and the path `/cognitiveservices/v1`
and again `/tts/cognitiveservices/v1`, with `style="happiness"` and with a style Emily lacks, and
`GET …/tts/cognitiveservices/voices/list` (look for `MAI`). Write down for each: the status, the headers
the service returned, whether the audio plays, and the exact voice names the list gives.

## 6. Decided (the owner, 2026-10-29)

1. **The prefix is `azure-speech-service:`**, the service's full name: a script says which service speaks,
   `Voice | azure-speech-service:en-GB-Emily:MAI-Voice-2.1` (A1).
2. **The settings join `voice.secrets.json`** (A2).
3. **The key**, not Microsoft Entra ID (A3); Entra is not planned.
4. **HD voices and pronunciation**: an HD voice passes through by name; pronunciation of product names is the
   [SSML plan's](ssml-plan.md) (M5: `pronunciations.md`, `sub`, `phoneme`).
5. **The sample rate is a setting**: `speech.sampleRate`, 24000 (the default) or 48000, in the Presenter's
   part, the file and `BRUINET_SPEECH_SAMPLE_RATE`; part of the cache key; step 1 tries Emily at 48 kHz (A2, A4, A6).
6. **Rate and pitch** come with SSML, later: a `<prosody>` in a line ([ssml-plan.md](ssml-plan.md) M4), not a
   row of its own in the script.
7. **Emily** is the settings' default voice: `en-GB-Emily:MAI-Voice-2.1`.
8. **A preview voice in the videos: no, not yet.** Emily is a preview voice, so this reading holds until
   the owner says otherwise: she speaks the owner's own recordings, and no video that is published uses a
   preview voice until the preview ends and its terms are read; a generally available neural voice, named the
   same way, is the voice for those. *(To confirm: the answer to 8 and the choice of Emily in 7 meet here.)*

Built 2026-10-29: steps 2 to 7 and the docs of step 8, on the plan's assumptions (a WAV answer, the `/cognitiveservices/v1` path on a resource host, a style the voice lacks sent as it is). Not done: step 1 (the owner's, with a key: it confirms those assumptions, and `speech-results.md` records them), the Presenter's dogfood scenarios, and the owner's recording with Emily.

## 7. Risks

- **The endpoint form is wrong for a custom-subdomain resource.** Step 1 finds the working form before any code; A2 accepts the forms found.
- **The shared pieces break the first voice.** Step 2 changes no behaviour and keeps `CloudVoiceTests` green.
- **Saving one service erases the other's settings.** Step 3 tests it both ways.
- **SSML breaks on a line with `&` or `<`.** The escape is tested with such a line.
- **The key leaks.** The rules of plan.md V1 and settings-plan.md S3, with the same tests on the new section.
- **A key from another region.** The 401 message says so, as the service's page does.
- **A preview service changes or goes.** No SLA, and decision 8 keeps preview voices out of published videos: the voice name and styles may change. The settings hold the voice name, so one edit moves a script to another voice, and the Windows voice is the way back.
- **Cost of a long script.** The cache makes a re-recording free; the guide quotes the current prices.

## Sources

- [Text to speech REST API (Microsoft Learn)](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/rest-text-to-speech)
- [MAI-Voice-2.1 and MAI-Voice-2.1-Flash (Microsoft Learn)](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/mai-voices)
