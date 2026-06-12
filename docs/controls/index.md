# Control Object Index

Brinell controls wrap platform elements with a consistent, test-friendly API.

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

- Shared interfaces: `srcnew/Brinell.Core/Interfaces`
- Locators: `srcnew/Brinell.Core/Locators`
- MAUI controls: `srcnew/Brinell.Maui/Controls`
- WPF/WinForms controls: `srcnew/Brinell.Wpf`, `srcnew/Brinell.WinForms`
- Web controls: `srcnew/Brinell.Html`, `srcnew/Brinell.Html.Playwright`

See [Core Interfaces](interfaces.md).
