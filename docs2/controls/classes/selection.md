# Selection Controls

**Source of truth:** `srcnew/Brinell.Maui/Controls/Selection/`

## Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiPickerControl` | `ISelectorControlObject` | `Picker` |
| `MauiTabControl` | `ITabControlObject` | Tab items |

## Picker Behavior

- `SelectByText()` — Opens picker, finds item by text, taps to select
- `SelectByIndex()` — Opens picker, selects by position
- `GetItemTexts()` — Returns all available items
- Platform specifics vary (native picker dialogs on mobile vs dropdown on desktop)

## Tab Behavior

- `Click()` — Navigates to the tab
- `IsSelected()` — Whether this tab is the active one
- `Title` — Tab header text
- Tab finding may use XPath fallback when AutomationId is not exposed (see active/SPEC-023)
