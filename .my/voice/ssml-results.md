# SSML, step 1 results (Windows)

Run 2026-10-29 on this machine through `Windows.Media.SpeechSynthesis.SynthesizeSsmlToStreamAsync`, with the three
voices it has (David, Zira, Mark, all `en-US`), a small program in the scratchpad. The audio was not listened to:
an element counts as honoured when it changes the WAV's length against the same words without it, so one that
changes only pitch or loudness (`prosody pitch`, `prosody volume`) cannot be told this way.

| Element | Length against the plain line | Reading |
| --- | --- | --- |
| `break time="1s"` | +1.05 s (500 ms: +0.55 s; `strength="x-strong"`: +1.3 s) | honoured |
| `emphasis level="strong"` | +0.2 s | honoured |
| `prosody rate="x-slow"` | 3 times as long | honoured; pitch and volume are accepted, not measurable |
| `sub alias="Brew ee net"` | +0.26 s against the original word | honoured: the alias is spoken |
| `p` and `s` | +0.7 to +1 s | honoured: they pause |
| `phoneme alphabet="ipa"` | the same as the plain word | accepted; not shown to change anything here |
| `say-as` `cardinal` against `digits` | identical length | not honoured: read as plain text |
| `lang xml:lang="nl-NL"` | identical to the plain line | not honoured |
| an unknown tag (`<brek/>`), `mstts:express-as`, `voice` | accepted, nothing spoken for the tag | ignored, words kept |
| `xml:lang="zz-ZZ"` on `speak` | an error | the language must be a real one: the voice's own (`Language`) |

So the Windows element set (M3) is `break`, `emphasis`, `prosody`, `sub`, `p`, `s` and `phoneme`; `say-as`, `lang`
and the `mstts:` elements are dropped there with their words kept (`VID023`). The document's `xml:lang` is the
chosen voice's `Language`, never a guess.
