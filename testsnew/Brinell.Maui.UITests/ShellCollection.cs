namespace Brinell.Maui.UITests;

/// <summary>
/// Collection definition sharing one <see cref="ShellFixture"/> - and so one running Shell
/// app - across the Shell test classes.
/// </summary>
[CollectionDefinition("Shell")]
public class ShellCollection : ICollectionFixture<ShellFixture>
{
}
