namespace Brinell.NativeAndroid;

public interface INativeAndroidContainer<TParent, out TSelf> :
    INativeAndroidScope<TSelf>,
    IContainerControl<NativeAndroidElement>
    where TParent : INativeAndroidScope<TParent>
{
    TParent Parent { get; }
}
