# Todo apps

The apps a mixed test or scenario can use, all on one backend. The UI tests in
`Bruinet.Samples.Todo.Mixed.UITests` and the scenarios in `Bruinet.Samples.Todo.Mixed.Uat` read
this one table. `Name` is what a test or step calls the app, and `User` is who uses it: a step
can say `Daan taps Save`.

| Column | Meaning |
| --- | --- |
| `Head` | `maui` or `web`: whose page objects drive the app. |
| `Platform` | For a MAUI app: `Windows` or `Android`. |
| `ClientId` | What the app sends in `X-Todo-Client`, so the backend can tell the apps apart. |
| `ClockOffsetMinutes` | How far the app's pinned clock is ahead of the test set's, so two edits of one todo are not at the same instant. |

## Apps

| Name | User | Head | Platform | ClientId | ClockOffsetMinutes |
| --- | --- | --- | --- | --- | --- |
| Daan's PC | Daan | maui | Windows | maui-daan | |
| Gus's PC | Gus | maui | Windows | maui-gus | 5 |
| Eva's phone | Eva | maui | Android | android-eva | |
| Finn's web | Finn | web | | web-finn | 10 |
| Hana's web | Hana | web | | web-hana | 15 |
