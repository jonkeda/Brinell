# Test Writing

Brinell tests should read like user workflows. Put mechanics in page objects and
controls.

## Test Shape

```csharp
[Fact]
public void SavingValidForm_ShowsSuccess()
{
    var page = _fixture.OpenFormPage();

    page.FillRequiredFields("Example");
    page.Save();

    Assert.True(page.SuccessMessage.IsVisible());
}
```

## Page Object Rules

- Page objects expose page-level intent.
- Page objects may expose controls when tests need direct assertions.
- Page objects should not contain broad recovery or cleanup loops.
- Navigation methods should wait for the next stable page state.

## Control Rules

- Use Brinell controls instead of raw driver elements.
- Move repeated interaction behavior into controls.
- Prefer semantic methods over driver-specific APIs.
- Keep app-specific assertions in tests, not controls.

## Waiting

Good waits name the state they expect:

- page is loaded;
- control is visible or enabled;
- text equals or contains an expected value;
- collection count changes;
- busy state ends;
- request or navigation completes.

Avoid:

- `Thread.Sleep` as a fix;
- unbounded retries;
- increasing timeouts without diagnostics;
- catching and ignoring platform exceptions.

## Assertions

Use xUnit `Assert`.

```csharp
Assert.True(page.SubmitButton.IsEnabled());
Assert.Equal("Saved", page.Status.GetText());
```

Do not add FluentAssertions to Brinell test projects.

## Naming

- Test class: `<Feature>Tests`.
- Test method: `<Action>_<Scenario>_<ExpectedResult>`.
- Page object: `<PageName>Page`.
- Fixture: `<PlatformOrApp>Fixture`.

## Artifacts

When a test produces screenshots, logs, UAT output, traces, or videos, route
them through the Brinell artifact layout under `TestResults/<run-id>/`.
