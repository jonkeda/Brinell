# SPX-015: Scope-Aware Fluent Chaining

**Status:** Active design for srcnew/Brinell.Maui

## Concept

Introduce `TScope` generic parameter for fluent method chaining. Controls return the scope (page/container) they belong to, enabling:

```csharp
page.UserName.Enter("test")
    .Password.Enter("pass")
    .Submit.Click();
```

## Key Interfaces

| Interface | Purpose |
|-----------|---------|
| `IMauiScope<TScope>` | Base scope providing fluent return type |
| `IMauiPage<TSelf>` | Page as a self-referencing scope |
| `IMauiContainer<TParent, TSelf>` | Container scope with parent navigation |

## Container Parent Navigation

Containers expose `.Parent` to navigate back up:

```csharp
page.UserProfile.Name.AssertText("John")
    .Parent  // back to page level
    .Submit.Click();
```

## Design Decisions

- Renamed `TPage` → `TScope` throughout to support containers as scopes
- `MauiControlBase<TScope>` — all controls generic over their scope
- `MauiContainerBase<TParent, TSelf>` — containers are scopes with parent reference
- Page-level `Self` property enables self-referencing without casting

## Tasks

1. Rename `TPage` → `TScope` in all base classes
2. Add `IMauiScope<TScope>` interface
3. Refactor `MauiControlBase<TScope>` constructor to accept scope
4. Add `MauiContainerBase<TParent, TSelf>` with `.Parent`
5. Update `MauiPageBase<TSelf>` as self-scope
6. Migrate all existing controls to new base
7. Update all test page objects
