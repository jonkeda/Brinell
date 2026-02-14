# Container Controls

**Source of truth:** `srcnew/Brinell.Maui/Controls/Container/`

## Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiContainerBase<TScope>` | `IContainerControl<IMauiElement>` | Any container element |
| `MauiFrameControl` | Container | `Frame` / `Border` |

## Container Scoping Pattern

Containers restrict element search to their subtree. Child controls find elements relative to the container root, not the page root.

### Key design:
- Container has a `ContainerRoot` element
- Child controls are initialized in the container constructor with the container as their scope
- `FindElement()` searches within container bounds only
- Nested containers create deeper scopes

### Usage pattern:
```csharp
public class UserProfileContainer : MauiContainerBase<MyPage>
{
    public MauiLabelControl<MyPage> Name { get; }
    public MauiEntryControl<MyPage> Email { get; }
    
    public UserProfileContainer(MyPage page, Locator locator)
        : base(page, locator)
    {
        Name = new(page, Locator.ByAutomationId("UserName"), this);
        Email = new(page, Locator.ByAutomationId("UserEmail"), this);
    }
}
```
