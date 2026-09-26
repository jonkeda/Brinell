# Repeat run: testsnew\Brinell.Html.UITests

5 runs, 36 tests, 1 min. Filter: (none).

The class column is a suggestion from the failure message, to sort the list. Decide the class by reading the failure.

| Test | Failed | Kind | Suggested class | First message |
|---|---:|---|---|---|
| `Brinell.Html.UITests.Tests.Scenarios.LoginFlowTests.LoginFlow_EmptyCredentials_ShowsError` | 5/5 | always | app-bug-or-test | Assert.NotEmpty() Failure: Collection was empty |
| `Brinell.Html.UITests.Tests.Scenarios.LoginFlowTests.LoginFlow_EmptyCredentials_ShowsError_Async` | 4/5 | flaky | app-bug-or-test | Assert.NotEmpty() Failure: Collection was empty |

## Slowest tests

Mean and worst over the runs, in seconds.

| Test | Mean | Worst |
|---|---:|---:|
| `Brinell.Html.UITests.Tests.Pages.LoginPageTests.Login_InvalidCredentials_ShowsErrorMessage` | 2.48 | 3.19 |
| `Brinell.Html.UITests.Tests.Scenarios.LoginFlowTests.LoginFlow_ValidCredentials_ShowsSuccessMessage_Async` | 2.24 | 2.26 |
| `Brinell.Html.UITests.Tests.Pages.LoginPageTests.Login_InvalidCredentials_ShowsErrorMessage_Async` | 2.22 | 2.24 |
| `Brinell.Html.UITests.Tests.Scenarios.LoginFlowTests.LoginFlow_ValidCredentials_ShowsSuccessMessage` | 2.21 | 2.23 |
| `Brinell.Html.UITests.Tests.Pages.LoginPageTests.Login_ValidCredentials_ShowsSuccessMessage` | 2.20 | 2.22 |
