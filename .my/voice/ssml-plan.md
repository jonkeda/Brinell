# SSML in the video scripts

Status: **plan**, not started. Date: 2026-10-29.

Follows [plan.md](plan.md) (the Azure OpenAI voice, built), [settings-plan.md](settings-plan.md) (built)
and [speech-plan.md](speech-plan.md) (Azure Speech, planned). A video script says what is spoken at each
step; this lets its author say *how*: a pause after a sentence, a word stressed, a product name
pronounced, a number read as digits.

## 1. Goal

A `Say` cell, or a scene's intro or outro paragraph, may hold SSML inline:

```markdown
| When I tap Save | and save.<break time="400ms"/> Your <emphasis level="strong">todo</emphasis> is on the list. |
| Given I am on the list | Welcome to <sub alias="Brew-ee-net">Bruinet</sub>, in <say-as interpret-as="cardinal">3</say-as> steps. |
```

Every script that has no markup keeps working unchanged. The same script works with every voice as far as
the voice can follow it: Windows' voices and Azure Speech speak the markup; Azure OpenAI's cannot, so
its markup is reduced to plain words and the script check says so. Captions, the review page and the
preview always show the plain text.

Pronunciations are the larger win: a suite lists how its names are said once, and the narrator marks
every line for it, so no script is cluttered with tags.

## 2. What holds today

| Fact | Where |
| --- | --- |
| A script's spoken text is a `Say` cell, or an intro or outro paragraph, as plain text; any heading other than a scene ends the scene. | `BddVideoScriptParser.cs` |
| The narrator makes every clip before the run: `Speech.ClipAsync(synthesizer, text, voice, wav)`; the clip's length comes from the WAV, so a pause changes the pace by itself. | `BddNarrationPacer.cs`, `Speech.cs` |
| An `ISpeechSynthesizer` takes plain text: `SynthesizeAsync(text, voice, wavPath)`. `WindowsSpeech` calls `SynthesizeTextToStreamAsync`; `AzureOpenAiSpeech` posts the text with `instructions`. | `Speech.cs`, `WindowsSpeech.cs`, `AzureOpenAiSpeech.cs` |
| The captions, the timeline and the review page take their text from `SpeechClip.Text`. | `BddNarrationPacer.cs` (`BddTimedClip`), `BddVideoComposer.cs` |
| A kept scene is out of date when its hash changes; the hash covers the text, voice, beat and, only when set, the instructions, so older scenes stay valid. | `BddKeptScenes.Hash` |
| The script check (`VID001` to `VID014`) runs in the language server and the preview. | `BddVideoScriptCheck.cs`, `BddVideoScriptPreviewWriter.cs` |

## 3. The services (Microsoft Learn)

| Voice | SSML |
| --- | --- |
| Windows (`Windows.Media.SpeechSynthesis`) | `SynthesizeSsmlToStreamAsync(ssml)` takes SSML 1.1: voice, pronunciation, volume, pitch, rate, emphasis. Which elements a given Windows voice honours is not listed; step 1 measures it. |
| Azure Speech | The `speak` and `voice` root, and `break` (up to 20,000 ms), `emphasis`, `prosody`, `say-as`, `sub`, `phoneme`, `p`, `s`, `lang`, `bookmark` (events need the SDK, not REST), and `mstts:` extras (`express-as`, `silence`, `backgroundaudio`). "Some voices don't support all tags": HD, personal and embedded voices. Step 1 checks the MAI voices. |
| Azure OpenAI `gpt-4o-mini-tts` | No SSML: plain text and `instructions`. |

## 4. Decisions

### M1. Inline fragments; the narrator writes the document

The author writes only the inside of a line. The narrator adds `speak`, the language and the voice from the
script's `Voice` (as plan.md and speech-plan.md already name it), so a script never repeats them and
cannot disagree with its own `Voice` row. A line is markup when it holds a tag whose name is a known SSML
element; it must then be well-formed. A line with `<` that starts no known element is plain text ("x < 5"),
and a tag-like word that is not an element (`<brek/>`) is a warning, so a typo is never spoken aloud.
Plain text is XML-escaped when the SSML is built (`Tom & Jerry` stays safe beside a break).

### M2. One neutral line, three renderings

`Bruinet.Narrator` parses a line into a small tree (`SpokenLine`) once and renders it three ways:

| Rendering | Used for |
| --- | --- |
| **Plain** | Captions, the timeline, the review page, the preview: the text without tags; a `sub` shows its content, not its alias; a break shows nothing. |
| **SSML** | A synthesizer that speaks it: the elements it supports kept, the others dropped with their content kept. |
| **Reduced** | A synthesizer that does not: plain text with each `sub` replaced by its alias. |

`SpeechClip.Text` becomes the plain rendering, so no caption ever shows a tag.

### M3. A synthesizer says what it understands

An optional interface `ISsmlSynthesizer : ISpeechSynthesizer` with `SynthesizeSsmlAsync(ssml, voice, wavPath, ct)`
and the set of elements it supports. `WindowsSpeech` and Azure Speech implement it; `AzureOpenAiSpeech`
does not and gets the reduced line. The same capability table, by the voice's prefix (none, `azure-speech-service:`,
`azure:`), feeds the script check without a synthesizer in hand.

### M4. v1 elements

`break`, `emphasis`, `prosody` (`rate`, `pitch`, `volume`), `say-as`, `sub`, `phoneme`, `p`, `s`, `lang`, and for
Azure Speech `mstts:express-as` and `mstts:silence`. Not in v1: `voice` (one voice per script; a second
speaker later), `audio` and `lexicon` (they fetch URLs; scripts stay offline), `bookmark` and `mark`
(needs the SDK's events), `mstts:backgroundaudio` (the composer owns the sound), visemes, personal voice.

### M5. Pronunciations once per suite

An optional `pronunciations.md` beside `uat.config.md`: a table `Term | Say | IPA`. Before rendering, the
narrator wraps each whole-word, case-sensitive occurrence of a term in plain text in `<sub alias="Say">` (or
`<phoneme alphabet="ipa" ph="IPA">` where a voice supports it and no `Say` is given). `sub` is the
portable one: it reaches every voice, Azure OpenAI's included, as its alias. Terms inside a tag the author
wrote are left alone. A script's own `## Pronunciations` section may override it later (Q2).

### M6. A kept scene notices

`BddKeptScenes.Hash` adds the pronunciations' text only when there are any, as it does for instructions,
so no scene kept before this is out of date. Markup is already in the hashed `Say` text.

### M7. Tone and style meet

For an Azure Speech voice, the script's `Instructions` row stays a style (speech-plan.md A5) and wraps the
whole line in `mstts:express-as`; a line that has its own `express-as` wins for that line.

### M8. The check

New codes after `VID014`:

| Code | Says |
| --- | --- |
| `VID020` | Error: the markup is not well-formed, at its position |
| `VID021` | Warning: a tag-like word is not an SSML element (escape it as `&lt;` to say it) |
| `VID022` | Error: a bad attribute: a `break` over 20,000 ms or not `500ms` or `2s`, an unknown `prosody` value, a `say-as` the voices do not know |
| `VID023` | Warning: the script's voice does not support this element; it is dropped, its words are kept |
| `VID024` | Error: an element where it may not go (a tag inside `say-as`, `sub` or `phoneme`) |
| `VID025` | Error: `pronunciations.md` has a term twice, or a row with nothing to say |

## 5. Steps

Each step one commit, with tests; no test calls a cloud service.

| # | Step | Done when |
| --- | --- | --- |
| 1 | **Spike on Windows, no key**: a small program synthesizes `<break>`, `emphasis`, `prosody`, `say-as`, `sub` and `phoneme` with this machine's voices and the language of the voice; the WAV lengths and a listen say which elements change the audio. Findings in `ssml-results.md`. | The Windows element set (M3) and the right `xml:lang` are known |
| 2 | **`SpokenLine`**: parse, the three renderings, escaping, the known-element rule (M1, M2). Pure code in `Bruinet.Narrator`. | Tests: tags, nesting, `Tom & Jerry`, `x < 5`, `<brek/>`, `sub` caption and alias, a plain line unchanged |
| 3 | **The check** (M8) in `BddVideoScriptCheck`, with the capability table by voice prefix; the preview shows plain text and marks a line that has markup. | The new codes in the language server's diagnostics; the preview tests |
| 4 | **Windows speaks it**: `ISsmlSynthesizer` on `WindowsSpeech`; the narrator renders SSML or reduced; `SpeechClip.Text` is plain; M6's hash. | A Windows test: a line with `<break time="1s"/>` is a second longer than without; captions hold no tag; an unchanged script's hash is unchanged |
| 5 | **Pronunciations** (M5): the reader, the rewrite, `VID025`. | Tests: a term wrapped, a term inside a tag left alone, the portable alias for a reduced voice |
| 6 | **Azure Speech** (needs speech-plan.md step 4): the SSML builder takes a `SpokenLine`, the `Instructions` style (M7), the `mstts:` elements. | Fake-handler tests: the exact body for a line with a break, an emphasis and a pronunciation; a dropped element named by `VID023` |
| 7 | **Azure OpenAI reduced**, and the cloud services' unsupported elements dropped with a warning at record time. | Tests: tags gone, aliases spoken, the cache key from the reduced text |
| 8 | **Docs and the sample**: the guide's script table, `VID020` to `VID025`; Todo's two scripts get a pause or two and a `pronunciations.md`; the owner records one. | The video pauses where the script says; a second recording is the same where nothing changed |

## 6. Open questions

| # | Question | Leaning |
| --- | --- | --- |
| Q1 | Inline fragments, or an explicit `Markup` field? | Inline with the known-element rule (M1): older scripts and plain lines need no change. |
| Q2 | Pronunciations in a suite file only, or also a script section? | The suite file first: names repeat across scripts. A script section later, for one video's exception. |
| Q3 | A pause on a voice without SSML: drop it, or split the line and join the clips with silence? | Drop first (M2); splitting costs a request per piece and loses the sentence's tune. Revisit if Azure OpenAI is the voice that matters. |
| Q4 | A second speaker with `<voice>`? | Later: one voice per script keeps the cache, the hash and the settings simple. |
| Q5 | A foreign word in a narration (`<lang xml:lang="en-US">`)? | In v1 for voices that take it; the check warns where one does not (M8). |
| Q6 | Map `prosody` and `emphasis` to per-line `instructions` for Azure OpenAI? | Not now; plan.md V4 already has one script-wide tone. |

## 7. Risks

- **Clutter in a Markdown table.** Pronunciations take the common case out of the lines; breaks stay short. GitHub's and VS Code's Markdown preview hide raw tags, so the Bruinet preview is where a script reads as written.
- **Windows' subset is unknown.** Step 1 first; a tag Windows ignores is `VID023`, never silently wrong.
- **A voice drops a tag the author relied on.** The capability table and `VID023` say so before recording, not after.
- **Escaping.** Plain text is escaped in one place, tested with `&`, `<` and quotes.
- **The hash.** Appended only when non-empty, so kept scenes stay valid, as for instructions.
- **speech-plan.md moves.** Step 6 waits for it; steps 1 to 5 and 7 do not.

## Sources

- [SpeechSynthesizer.SynthesizeSsmlToStreamAsync (Microsoft Learn)](https://learn.microsoft.com/en-us/uwp/api/windows.media.speechsynthesis.speechsynthesizer.synthesizessmltostreamasync)
- [SSML document structure and events (Microsoft Learn)](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/speech-synthesis-markup-structure)
