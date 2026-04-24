# Container Testing

**Status:** Active | **Version:** 2.0 | **Supersedes:** SPEC-017-CONTAINER v1

## Test Structure

ContainerDemoPage with 4 sections:
1. **UserProfile** — Single container with scoped child controls
2. **Outer → Inner** — Nested containers (2 levels deep)
3. **TaskList** — CollectionView with `MauiListControl<TScope, TItem>`
4. **Contacts** — Indexed containers (multiple same-type containers)

## Key Test Patterns

### Controls as Properties
```csharp
public class ContainerDemoPage : MauiPageBase<IMauiElement>
{
    public UserProfileContainer UserProfile { get; }
    public MauiListControl<ContainerDemoPage, TaskItem> Tasks { get; }
}
```

### Container Constructor Pattern
```csharp
public class UserProfileContainer : MauiContainerBase<ContainerDemoPage>
{
    public MauiLabelControl<ContainerDemoPage> Name { get; }
    
    public UserProfileContainer(ContainerDemoPage page, Locator locator)
        : base(page, locator)
    {
        Name = new(page, Locator.ByAutomationId("UserName"), this);
    }
}
```

## Test Suites

| Suite | Tests | Validates |
|-------|-------|-----------|
| SingleContainerTests | ~5 | Scoped search within one container |
| NestedContainerTests | ~5 | Inner container searches within outer |
| ListContainerTests | ~5 | Typed list items, count, iteration |
| IndexedContainerTests | ~5 | Multiple containers of same type by index |
| ContainerScopingTests | ~5 | Boundary isolation (no cross-container leaks) |

## Rules

- Use xUnit `Assert` only (no FluentAssertions)
- Controls initialized in constructor, not in test methods
- Containers receive parent scope for element search boundary
