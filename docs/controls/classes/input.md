# Input / Text Controls

**Source of truth:** `srcnew/Brinell.Maui/Controls/Text/`, `srcnew/Brinell.Maui/Controls/Buttons/`

## Button Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiButtonControl` | `IClickableControlObject` | `Button` |
| `MauiImageButtonControl` | `IClickableControlObject` | `ImageButton` |

Buttons implement click, double-click, long-press. `ScrollIntoView` before click if off-screen.

## Text Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiEntryControl` | `IEditableTextControlObject` | `Entry` |
| `MauiEditorControl` | `IEditableTextControlObject` | `Editor` |
| `MauiSearchBarControl` | `IEditableTextControlObject` | `SearchBar` |
| `MauiLabelControl` | `ITextControlObject` | `Label` |
| `MauiSpanControl` | `ITextControlObject` | `Span` |

### Text Input Methods

Via `TextInputMethod` enum:
- **Keys** — Type character by character (default)
- **Paste** — Clipboard paste
- **SetValue** — Direct value property set

`Enter()` clears existing text first, then inputs. `Append()` does not clear.
`SetText()` uses the fastest method available for the platform.
