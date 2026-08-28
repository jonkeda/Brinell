namespace Brinell.Core.Interfaces;

/// <summary>
/// Marks an <c>Is*Core</c> query as meaningful when the element is absent.
/// </summary>
/// <remarks>
/// <para>
/// Generated <c>Wait*</c> and <c>Assert*</c> members normally resolve the element before
/// evaluating the predicate, so a missing element raises
/// <c>ElementNotFoundException</c> rather than failing the comparison. That is correct for
/// a value assertion — "the label is gone" is not a passing result for a text check — but
/// it makes absence inexpressible for the two queries that are *about* presence.
/// </para>
/// <para>
/// Applying this attribute makes the generator emit the null-tolerant helpers instead, so
/// the element is resolved optionally, visibility is not forced, and the Core method
/// decides. Apply it only to queries whose Core method accepts a null element and returns
/// a meaningful answer for one — <c>IsExistsCore</c> and <c>IsVisibleCore</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AbsenceTolerantAttribute : Attribute
{
}
