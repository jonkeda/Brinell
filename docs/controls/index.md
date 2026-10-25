# Control Object Index

Bruinell controls wrap platform elements with a consistent, test-friendly API.

## Control Responsibilities

Controls should:

- expose semantic actions such as `Click`, `SetText`, `SelectItem`, and
  `WaitReady`;
- hide driver-specific locators and element details;
- wait for state when performing actions;
- provide clear assertions and diagnostics;
- avoid pointer or coordinate APIs for routine interactions.

Controls should not:

- know test-specific business assertions;
- depend on app-specific page objects;
- use arbitrary sleeps to hide race conditions;
- swallow driver failures without context.

## Common Control Families

| Family | Examples |
| --- | --- |
| Foundation | base controls, containers, pages, contexts |
| Input | entries, text boxes, buttons |
| Toggle | checkboxes, switches |
| Selection | pickers, combo boxes, radio groups |
| Collection | lists, tables, item containers |
| Range | sliders, steppers, progress |
| Display | labels, images, status text |
| Navigation | tabs, menus, links |
| Media | image/video style controls |

## Source Of Truth

- Shared interfaces: `srcnew/Bruinell.Core/Interfaces`
- Locators: `srcnew/Bruinell.Core/Locators`
- MAUI controls: `srcnew/Bruinell.Maui/Controls`
- CommunityToolkit.Maui controls: `srcnew/Bruinell.Maui.CommunityToolkit/Controls` - see
  [CommunityToolkit.Maui Controls](community-toolkit.md); for another library, see
  [Adding Controls For A New Control Library](../../.guides/control-library.md)
- WPF/WinForms controls: `srcnew/Bruinell.Wpf`, `srcnew/Bruinell.WinForms`
- Web controls: `srcnew/Bruinell.Html/Controls` and `srcnew/Bruinell.Blazor/Controls`. They are
  generated from `.tpl.cs` templates like MAUI's and implement the same Core interfaces, so a
  member has the same name on both stacks. The Playwright driver is `srcnew/Bruinell.Html.Playwright`.
- Native Android controls: `srcnew/Bruinell.NativeAndroid/Controls`, generated the same way, with
  Android's widget names (`EditText`, `Spinner`, `RecyclerView`) and the same members and UAT
  verbs. See the [native Android guide](../platform-guides/native-android.md).

See [Core Interfaces](interfaces.md).
