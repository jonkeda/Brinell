# Blazor Sample App Design

**Source of truth:** `samples/Brinell.Samples.Blazor.App/`

## Purpose

Sample Blazor app exercising all SPEC-006 interfaces for Playwright-based UI testing.

## Interface-to-Component Mapping

| Interface | HTML/Blazor Component | Sample Page |
|-----------|----------------------|-------------|
| `IClickableControlObject` | `<button>`, `<a>` | Buttons page |
| `IEditableTextControlObject` | `InputText`, `<textarea>` | Text page |
| `ITextControlObject` | `<span>`, `<p>`, `<label>` | Display page |
| `IToggleControlObject` | `InputCheckbox`, `<input type="checkbox">` | Toggle page |
| `ISelectorControlObject` | `InputSelect`, `<select>` | Selection page |
| `IRangeControlObject` | `<input type="range">` | Range page |
| `IProgressControlObject` | `<progress>` | Range page |
| `IDateControlObject` | `InputDate` | DateTime page |
| `ITimeControlObject` | `<input type="time">` | DateTime page |
| `IScrollableControlObject` | `<div>` with overflow | Collection page |
| `IContainerControl` | `<div>`, `<section>`, `<fieldset>` | Container page |
| `IExpandableControlObject` | `<details>/<summary>` | Advanced page |

## Status

Blazor platform implementation is scaffolded (`Placeholder.cs`). Sample app exists but controls are not yet wired to Brinell interfaces.
