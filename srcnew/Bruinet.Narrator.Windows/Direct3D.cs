using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace Bruinet.Recording.Windows;

// The interop Windows.Graphics.Capture needs from .NET: a Direct3D device as WinRT sees it, and a
// capture item for a window or a monitor handle.
internal static class Direct3D
{
    private const int HardwareDriver = 1;
    private const uint BgraSupport = 0x20;
    private const uint SdkVersion = 7;

    private static readonly Guid DxgiDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
    private static readonly Guid CaptureItem = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    public static IDirect3DDevice CreateDevice()
    {
        Check(D3D11CreateDevice(0, HardwareDriver, 0, BgraSupport, 0, 0, SdkVersion, out var device, out _, out var context), "D3D11CreateDevice");
        try
        {
            Check(Marshal.QueryInterface(device, in DxgiDevice, out var dxgi), "IDXGIDevice");
            try
            {
                Check(CreateDirect3D11DeviceFromDXGIDevice(dxgi, out var inspectable), "CreateDirect3D11DeviceFromDXGIDevice");
                try
                {
                    return MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
                }
                finally
                {
                    Marshal.Release(inspectable);
                }
            }
            finally
            {
                Marshal.Release(dxgi);
            }
        }
        finally
        {
            Marshal.Release(context);
            Marshal.Release(device);
        }
    }

    public static GraphicsCaptureItem ForWindow(nint window)
        => FromInterop(interop => interop.CreateForWindow(window, CaptureItem));

    public static GraphicsCaptureItem ForMonitor(nint monitor)
        => FromInterop(interop => interop.CreateForMonitor(monitor, CaptureItem));

    private static GraphicsCaptureItem FromInterop(Func<IGraphicsCaptureItemInterop, nint> create)
    {
        var factory = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
        var interop = (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factory.ThisPtr);
        var item = create(interop);
        try
        {
            return GraphicsCaptureItem.FromAbi(item);
        }
        finally
        {
            Marshal.Release(item);
        }
    }

    private static void Check(int result, string call)
    {
        if (result < 0)
        {
            throw new InvalidOperationException($"{call} failed: 0x{result:X8}.", Marshal.GetExceptionForHR(result));
        }
    }

    [DllImport("d3d11.dll")]
    private static extern int D3D11CreateDevice(
        nint adapter, int driverType, nint software, uint flags, nint featureLevels, uint featureLevelCount,
        uint sdkVersion, out nint device, out int featureLevel, out nint immediateContext);

    [DllImport("d3d11.dll")]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(nint dxgiDevice, out nint graphicsDevice);

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        nint CreateForWindow([In] nint window, [In] in Guid iid);

        nint CreateForMonitor([In] nint monitor, [In] in Guid iid);
    }
}
