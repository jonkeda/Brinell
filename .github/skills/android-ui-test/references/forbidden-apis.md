# Forbidden in native Android tests

In test methods, page objects' public members and test helpers, as on MAUI
([maui-ui-test forbidden-apis.md](../../maui-ui-test/references/forbidden-apis.md), which gives the
reasons) and Html:

| Forbidden | Use instead |
| --- | --- |
| `Context.Driver` and any `INativeAndroidDriver` member | a page-object member, or `GalleryFixture.Open(screen)` for a deep link |
| `INativeAndroidElement` variables, parameters, returns or members | a ControlObject |
| `FindElement`, `FindElements`, `TryFindElement`, `TryGetItemRoot` | a control, container or collection member |
| `new Locator(...)` / `Locator.By...` in a test method | a control declared on the page object |
| `AndroidDriver`, `AppiumElement`, `By`, `MobileBy` and other Appium or Selenium types | the stack's own types |
| `adb` or `mobile:` commands from a test | a page-object or fixture member |
| `Thread.Sleep`, `Task.Delay`, a hand-written wait loop | an `Assert*` or `Wait*` member, or the app's busy view |

The one exception is the stack's own adapter tests (`AppiumErrorMappingTests`, the locator
tests), which test the adapter itself.
