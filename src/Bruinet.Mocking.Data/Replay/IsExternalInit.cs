#if !NET
namespace System.Runtime.CompilerServices;

/// <summary>Lets the netstandard2.0 build use records and <c>init</c>: the compiler looks for this type by name.</summary>
internal static class IsExternalInit
{
}
#endif
