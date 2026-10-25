namespace Bruinell.NativeAndroid.Interfaces;

/// <summary>A scope inside another scope, found by its own root view.</summary>
/// <typeparam name="TParent">The scope it is declared in.</typeparam>
/// <typeparam name="TSelf">The container's own type.</typeparam>
public interface INativeAndroidContainer<TParent, TSelf> : INativeAndroidScope<TSelf>
    where TParent : INativeAndroidScope<TParent>
    where TSelf : INativeAndroidContainer<TParent, TSelf>
{
    /// <summary>The scope the container is declared in.</summary>
    TParent Parent { get; }
}
