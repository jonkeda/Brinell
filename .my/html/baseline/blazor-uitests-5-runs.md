# Repeat run: testsnew\Brinell.Blazor.UITests

5 runs, 31 tests, 1 min. Filter: (none).

The class column is a suggestion from the failure message, to sort the list. Decide the class by reading the failure.

| Test | Failed | Kind | Suggested class | First message |
|---|---:|---|---|---|
| `Brinell.Blazor.UITests.Tests.Controls.ButtonClickTests.Button_MultipleClicks_AllRegister` | 5/5 | always | app-bug-or-test | Brinell.Core.Exceptions.AssertionException : Text does not contain '3'. Actual: 'Current count: 2' |
| `Brinell.Blazor.UITests.Tests.Pages.CounterPageTests.Counter_CountDisplay_UpdatesAfterClick` | 5/5 | always | app-bug-or-test | Brinell.Core.Exceptions.AssertionException : Text does not contain '1'. Actual: 'Current count: 0' |
| `Brinell.Blazor.UITests.Tests.Pages.CounterPageTests.Counter_MultipleIncrements_AccumulatesCorrectly` | 5/5 | always | app-bug-or-test | Brinell.Core.Exceptions.AssertionException : Text does not contain '5'. Actual: 'Current count: 4' |

## Slowest tests

Mean and worst over the runs, in seconds.

| Test | Mean | Worst |
|---|---:|---:|
| `Brinell.Blazor.UITests.Tests.Pages.CounterPageTests.Counter_ClickReset_ResetsToZero` | 0.40 | 0.45 |
| `Brinell.Blazor.UITests.Tests.Pages.CounterPageTests.Counter_MultipleIncrements_AccumulatesCorrectly` | 0.33 | 0.37 |
| `Brinell.Blazor.UITests.Tests.Controls.ButtonClickTests.Button_MultipleClicks_AllRegister` | 0.27 | 0.30 |
| `Brinell.Blazor.UITests.Tests.Pages.CounterPageTests.Counter_CountDisplay_UpdatesAfterClick` | 0.26 | 0.64 |
| `Brinell.Blazor.UITests.Tests.Pages.CounterPageTests.Counter_ClickIncrement_IncreasesCount` | 0.25 | 0.58 |
