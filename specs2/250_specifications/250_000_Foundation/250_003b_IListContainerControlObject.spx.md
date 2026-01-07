# 250.003b IListContainerControlObject Specification

**Block Type:** SPC (Specification)  
**ID:** 250.003b  
**Title:** IListContainerControlObject Interface Specification  
**Status:** Draft  
**Version:** 1.1  
**Level:** 0 - Foundation

---

## 1. Overview

`IListContainerControlObject<T>` is a generic interface for container controls that hold multiple typed children. Like `IContainerControlObject<T>` with its `Child` property, this interface has a `Children` property that returns all items as a typed collection, plus an indexer for direct item access. The type parameter `T` must be a control that implements `IControlObject`.

Examples include `ListView`, `CollectionView`, `ItemsControl`, repeating elements, and data grids.

For single-content containers, see [IContainerControlObject<T>](250_003_IContainerControlObject.spx.md).

### Interface Identity

- **Package:** `Brinell.Core`
- **Namespace:** `Brinell.Core.Interfaces`
- **Dependencies:** `IControlObject`
- **Implementors:** `ListContainerControlBase<T>`, `ListViewControl<TItem>`, `CollectionViewControl<TItem>`, `ItemsControl<TItem>`

---

## 2. Behavior

### 2.1 Interface Definition

```csharp
/// <summary>
/// Container control with multiple typed children (list-based container).
/// Children are defined as a typed collection property.
/// </summary>
/// <typeparam name="T">Type of child control. Must implement IControlObject.</typeparam>
public interface IListContainerControlObject<T> : IControlObject where T : IControlObject
{
    /// <summary>
    /// Gets all child controls of this container.
    /// </summary>
    /// <remarks>
    /// This property returns the current snapshot of children.
    /// The collection may be empty if no children exist.
    /// Children are returned in DOM/visual tree order.
    /// </remarks>
    IReadOnlyList<T> Children { get; }
    
    /// <summary>
    /// Gets the child control at the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the child.</param>
    /// <returns>The child control at the index.</returns>
    /// <exception cref="IndexOutOfRangeException">If index is out of range.</exception>
    T this[int index] { get; }
    
    /// <summary>
    /// Gets the count of child elements.
    /// </summary>
    int Count { get; }
    
    /// <summary>
    /// Find the first child matching a predicate.
    /// </summary>
    /// <param name="predicate">Condition to match.</param>
    /// <returns>The first matching child, or null if none found.</returns>
    T? FirstOrDefault(Func<T, bool> predicate);
    
    /// <summary>
    /// Wait until child count matches expected value.
    /// </summary>
    /// <param name="expected">Expected count. Null = skip operation.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    bool WaitCount(int? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert child count equals expected value.
    /// </summary>
    /// <param name="expected">Expected count. Null = skip operation.</param>
    /// <param name="message">Custom failure message. Null = use default.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    void AssertCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

### 2.2 Typed Children Property

The `Children` property returns all items as a typed collection:

```csharp
// Container with typed children
public class ProductGrid : ListContainerControlBase<ProductCard>
{
    public ProductGrid(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    // Children property automatically returns ProductCard instances
    // No casting needed in test code
}

// Test code - Children is strongly typed
var grid = page.ProductGrid;
IReadOnlyList<ProductCard> products = grid.Children;  // Typed collection
ProductCard first = grid[0];                           // Typed indexer
```

**Behavior:**
- `Children` property returns current snapshot of all children
- Collection is typed as `IReadOnlyList<T>` for compile-time safety
- Indexer provides direct typed access to children by index
- No casting required - type flows from interface

### 2.3 Comparison with Non-Generic IContainerControl

| Feature | IListContainerControlObject<T> | IContainerControl |
|---------|--------------------------------|-------------------|
| **Child Access** | `Children` property (typed) | `FindControls<T>(locator)` |
| **Type Safety** | Compile-time | Runtime |
| **Item Access** | `this[index]` indexer | `FindControl<T>(locator)` |
| **Use Case** | Known homogeneous items | Unknown/dynamic content |
| **Example** | `ProductGrid : IListContainerControlObject<ProductCard>` | `Frame.FindControls<ButtonControl>(locator)` |

### 2.4 Inheritance Hierarchy

```
IControlObject
    ├── IContainerControlObject<T>      (single typed child)
    ├── IListContainerControlObject<T>  (multiple typed children)
    └── IContainerControl               (dynamic scoped finding)
```

---

## 3. Boundary

### 3.1 Container Not Found

| Scenario | Behavior |
|----------|----------|
| `Children` when container doesn't exist | Throws ElementNotFoundException |
| `this[0]` when container doesn't exist | Throws ElementNotFoundException |
| `Count` when container doesn't exist | Throws ElementNotFoundException |
| `IsExists()` (inherited) when container doesn't exist | Returns false |

### 3.2 Child Not Found

| Scenario | Behavior |
|----------|----------|
| `Children` when no children | Returns empty list |
| `this[5]` when only 3 children exist | Throws IndexOutOfRangeException |
| `Count` when no children | Returns 0 |
| `FirstOrDefault()` when no match | Returns null |

---

## 4. Acceptance Criteria

### ACC-001: Typed Children Access

```gherkin
Given a ProductGrid containing 5 ProductCard items
When Children property is accessed
Then it returns IReadOnlyList<ProductCard> with 5 items
And each item is a ProductCard instance (no casting needed)
```

### ACC-002: Indexer Access

```gherkin
Given a ProductGrid with 10 items
When grid[0] is accessed
Then it returns the first ProductCard

When grid[9] is accessed
Then it returns the last ProductCard

When grid[10] is accessed
Then it throws IndexOutOfRangeException
```

### ACC-003: Count Property

```gherkin
Given a ProductGrid with 10 items
When Count property is accessed
Then it returns 10

Given an empty ProductGrid
When Count property is accessed
Then it returns 0
```

### ACC-004: Predicate Search

```gherkin
Given a ProductGrid with items named "Widget", "Gadget", "Tool"
When FirstOrDefault(p => p.Name.GetText() == "Gadget") is called
Then it returns the ProductCard for "Gadget"

When FirstOrDefault(p => p.Name.GetText() == "NonExistent") is called
Then it returns null
```

### ACC-005: Wait for Count

```gherkin
Given a list where items load asynchronously
And 5 items appear after 500ms
And a timeout of 2000ms
When WaitCount(5, 2000) is called
Then it returns true after approximately 500ms
```

### ACC-006: LINQ Support

```gherkin
Given a ProductGrid with items priced $10, $20, $30
When Children.Where(p => p.Price > 15).ToList() is called
Then it returns 2 ProductCard items (those priced $20 and $30)
```

---

## 5. Assumptions

- **ASM-001:** Container holds homogeneous typed children
- **ASM-002:** Children are returned in DOM/visual tree order
- **ASM-003:** Index-based access is zero-based
- **ASM-004:** Virtual/recycled items handled by platform implementation
- **ASM-005:** Children property returns current state (snapshot)

---

## 6. Exclusions

- **EXC-001:** Virtual scrolling optimization — platform-specific
- **EXC-002:** Item selection — see ISelectorControlObject
- **EXC-003:** Heterogeneous children — use IContainerControl.FindControls
- **EXC-004:** Data binding awareness — tests see rendered UI only

---

## 7. Implementation Example

```csharp
public abstract class ListContainerControlBase<T> : ControlBase, IListContainerControlObject<T>
    where T : IControlObject
{
    private readonly Locator _itemLocator;
    
    protected ListContainerControlBase(
        ITestContext context, 
        Locator locator, 
        Locator itemLocator,
        IPageObject? page = null)
        : base(context, locator, page)
    {
        _itemLocator = itemLocator ?? Locator.AllChildren();
    }
    
    public IReadOnlyList<T> Children
    {
        get
        {
            var scopedLocator = _itemLocator.ScopedTo(Locator);
            var elements = FindScopedElements(scopedLocator);
            return elements.Select((e, i) => CreateChildControl(i, e)).ToList();
        }
    }
    
    public T this[int index]
    {
        get
        {
            var count = Count;
            if (index < 0 || index >= count)
                throw new IndexOutOfRangeException($"Index {index} is out of range. Count: {count}");
            
            return Children[index];
        }
    }
    
    public int Count => Children.Count;
    
    public T? FirstOrDefault(Func<T, bool> predicate)
    {
        return Children.FirstOrDefault(predicate);
    }
    
    public bool WaitCount(int? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(() => Count == expected.Value, timeout);
    }
    
    public void AssertCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitCount(expected, timeoutMs);
        var actual = Count;
        if (actual != expected.Value)
            throw new AssertionException(message ?? $"Expected {expected} children but found {actual}");
    }
    
    // Abstract - platform-specific child control creation
    protected abstract T CreateChildControl(int index, object element);
    protected abstract IReadOnlyList<object> FindScopedElements(Locator locator);
}
```

---

## 8. Usage Example

```csharp
// ProductCard - the child item type
public class ProductCard : ContainerControlBase
{
    public LabelControl Name { get; }
    public LabelControl Price { get; }
    public ButtonControl AddToCart { get; }
    
    public ProductCard(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        Name = new LabelControl(context, Locator.ByAutomationId("ProductName").ScopedTo(locator), page);
        Price = new LabelControl(context, Locator.ByAutomationId("ProductPrice").ScopedTo(locator), page);
        AddToCart = new ButtonControl(context, Locator.ByAutomationId("AddToCart").ScopedTo(locator), page);
    }
}

// ProductGrid - typed list container
public class ProductGrid : ListContainerControlBase<ProductCard>
{
    public ProductGrid(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, Locator.ByClassName("ProductCard"), page) { }
    
    protected override ProductCard CreateChildControl(int index, object element)
    {
        return new ProductCard(_context, Locator.ByIndex(index).ScopedTo(Locator), Page);
    }
}

// Page with typed list container
public class ProductListPage : PageObjectBase
{
    public ProductGrid Products { get; }
    
    public ProductListPage(IMauiTestContext context) : base(context, "ProductList")
    {
        Products = new ProductGrid(context, Locator.ByAutomationId("ProductGrid"), this);
    }
}

// Test using typed list container
[Test]
public void CanWorkWithTypedProductList()
{
    var page = new ProductListPage(context);
    page.WaitLoaded(true);
    
    // Typed collection - no casting needed
    IReadOnlyList<ProductCard> products = page.Products.Children;
    
    // Verify count
    page.Products.AssertCount(5);
    
    // Index access - returns ProductCard directly
    ProductCard firstProduct = page.Products[0];
    firstProduct.AddToCart.Click();
    
    // Find specific product - returns ProductCard?
    ProductCard? widget = page.Products.FirstOrDefault(p => 
        p.Name.GetText() == "Widget");
    widget?.AddToCart.Click();
    
    // LINQ on typed collection
    var expensiveProducts = page.Products.Children
        .Where(p => decimal.Parse(p.Price.GetText().TrimStart('$')) > 100)
        .ToList();
    
    foreach (ProductCard product in expensiveProducts)
    {
        Console.WriteLine($"Expensive: {product.Name.GetText()}");
    }
}
```

---

## Related Documents

- [IControlObject Specification](250_001_IControlObject.spx.md)
- [IContainerControlObject<T> Specification](250_003_IContainerControlObject.spx.md)
- [IContainerControl Specification](250_003a_IContainerControl.spx.md)
- [Container Pattern](../../200_architecture/231_Patterns/231_004_ContainerPattern.spx.md)
