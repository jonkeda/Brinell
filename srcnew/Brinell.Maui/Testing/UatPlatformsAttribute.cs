using Brinell.Maui.Enums;

namespace Brinell.Maui.Testing;

/// <summary>
/// The platforms a fixture can run on, for a tool that has to know before it runs anything.
/// </summary>
/// <remarks>
/// A fixture already answers this in code — <c>GetDefaultAppPath</c> throws
/// <see cref="NotSupportedException" /> for a platform it has no binary for — but that answer
/// arrives only once the fixture is constructed. Presenter offers the platform picker before
/// loading the assembly at all, reading metadata only, where an attribute's arguments are
/// readable and a property's value is not. Declaring nothing means "whatever the target
/// supports", and the fixture's own exception stays the error.
/// </remarks>
/// <param name="platforms">The platforms the fixture supports.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class UatPlatformsAttribute(params MauiPlatform[] platforms) : Attribute
{
    /// <summary>The platforms the fixture supports.</summary>
    public IReadOnlyList<MauiPlatform> Platforms { get; } = platforms;
}
