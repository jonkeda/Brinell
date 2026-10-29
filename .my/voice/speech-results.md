# Azure Speech, step 1 results

Run 2026-10-29 through the narrator's own `AzureSpeech` and `AzureSpeechVoices` (not curl), with the
owner's key on a Speech resource in `westeurope` (endpoint `https://westeurope.api.cognitive.microsoft.com/`).
The audio was not listened to; lengths and sizes only.

| Question (speech-plan section 2) | Found |
| --- | --- |
| Endpoint form | The resource's endpoint is the regional Cognitive Services one, not a text-to-speech host. It is mapped to `https://westeurope.tts.speech.microsoft.com`; synthesis `/cognitiveservices/v1` and `voices/list` both answer there with the key header. The custom-subdomain form (`*.cognitiveservices.azure.com`) and the Foundry resource are not tried. |
| Does Emily answer in a WAV? | Yes: `riff-24khz-16bit-mono-pcm`, 2.3 s for the sample line; `riff-48khz-16bit-mono-pcm` too (181 KB for 1.9 s). No raw PCM fallback is needed. |
| Is `westeurope` a MAI region? | Yes: Emily and Harry, both `MAI-Voice-2.1` and `-Flash`, are in the list, 974 voices in all. |
| Names in `voices/list` | The full name with the model: `en-GB-Emily:MAI-Voice-2.1`, `en-GB-Emily:MAI-Voice-2.1-Flash`, `en-GB-Harry:MAI-Voice-2.1`. Status `Preview`. A neural voice is `en-GB-SoniaNeural` (GA), an HD one `en-GB-Sonia:DragonHDLatestNeural`. |
| Emily's styles (list) | agent, angry, audiobook, confused, customer_call_center, disgusted, educational, embarrassed, excited, fearful, happy, jealous, joyful, narrator, sad, surprised. No `neutral` (the docs table has it). Harry has no `confused`, `embarrassed`, `excited`, `happy`. |
| A style the voice lacks | On Emily, `whispering` is a 400. On a neural voice (`en-GB-SoniaNeural`, whose list has only cheerful and sad) `narrator` is accepted and sounds the same length as none: ignored. |
| Free text as the style | Accepted on Emily with no error, but the clip is 6.7 s against 1.9 s: probably spoken aloud. Listen before trusting it; the plan's rule (a style name, a check warning) stands. |
| Cost of the run | Eight short clips and one list call. |
