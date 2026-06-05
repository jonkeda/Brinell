namespace Brinell.NativeAndroid;

public interface INativeAndroidScope<out TSelf> : IElementScope<NativeAndroidElement>
{
    NativeAndroidTestContext Context { get; }

    TSelf Self { get; }
}
