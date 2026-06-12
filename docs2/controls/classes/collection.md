# Collection Controls

**Source of truth:** `srcnew/Brinell.Maui/Controls/Collection/`

## Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiCollectionViewControl` | `IScrollableControlObject` | `CollectionView` |
| `MauiListViewControl` | `IScrollableControlObject` | `ListView` |
| `MauiListControl<TScope, TItem>` | Typed collection | Any list container |

## MauiListControl Pattern

Generic list control providing typed access to collection items:

```csharp
// Page object declares a typed list
public MauiListControl<TaskListPage, TaskItem> Tasks { get; }
```

- `TScope` — The page/scope that owns the list
- `TItem` — A container class representing each item
- Items are resolved lazily by index
- Supports iteration, count checking, and item-level scoped operations
