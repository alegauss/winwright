using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Winwright.Fixture;

/// <summary>
/// A Mica window whose image area is Direct3D 11 presenting into a child window. WW552.
/// <para>
/// Found by PhotoBoost (PB27, PB40): its comparator is a DXGI flip swapchain in an HwndHost, on a
/// window with ThemeMode Fluent. The screen copy is refused for the glass and a render of the visual
/// tree has a hole where the swapchain is, so the window had no capture route at all. The render now
/// copies a hosted region from its window, and this is the window that shows it does: the swapchain is
/// cleared to <see cref="Cleared"/>, a colour nothing else on the page draws.
/// </para>
/// <para>
/// Raw COM through the vtable rather than a wrapper, because the fixture takes what an adopter takes
/// and an adopter's Direct3D library is theirs to choose. Five calls are all it needs: the device and
/// swapchain, the back buffer, a view of it, the clear and the present.
/// </para>
/// </summary>
public static class SwapchainPane
{
    /// <summary>The size the page is laid out and rendered at.</summary>
    public static Size Size { get; } = new(240, 140);

    /// <summary>What the swapchain is cleared to, as red, green and blue bytes.</summary>
    public static (byte Red, byte Green, byte Blue) Cleared { get; } = (0x20, 0xA0, 0x60);

    /// <summary>
    /// Show the window, present one frame into the swapchain, render the page and close it again.
    /// Returns the render's own sentence, so the run prints what the receipt said.
    /// </summary>
    /// <param name="path">Where the picture goes.</param>
    public static string Rendered(string path)
    {
        var rows = new StackPanel { Name = "swapchainRows" };
        rows.Children.Add(new TextBlock { Name = "swapchainTitle", Text = "A comparator", Height = 24 });
        var chain = new Swapchain { Name = "swapchainImage", Width = 200, Height = 100, HorizontalAlignment = HorizontalAlignment.Left };
        rows.Children.Add(chain);
        var pane = new Border { Name = "swapchainPane", Child = rows, Width = Size.Width, Height = Size.Height };

        var window = new Window
        {
            Title = "winwright swapchain",
            Content = pane,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = 40,
            Top = 40,
            ShowActivated = false,
            ShowInTaskbar = false,
        };

        // The glass is half the shape: it is what refuses the screen copy, and so what leaves the
        // render as the only route there is.
        window.SourceInitialized += (_, _) => Backdrop.Set(new WindowInteropHelper(window).Handle, "mica");
        window.Show();
        Settle(TimeSpan.FromMilliseconds(200));

        chain.Present(Cleared);

        // Long enough for the compositor to take the frame up: a print asks it for what it composed,
        // and a frame presented a moment ago is not composed yet.
        Settle(TimeSpan.FromMilliseconds(500));

        try
        {
            return Winwright.InApp.Render.ToFile(pane, path, Protocol.Background(pane), Size).Sentence();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Run the dispatcher for that long, since this route never starts the application's loop.</summary>
    private static void Settle(TimeSpan span)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = span };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };

        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    /// <summary>A child window with a Direct3D 11 swapchain presenting into it.</summary>
    private sealed class Swapchain : HwndHost
    {
        private nint swap;
        private nint device;
        private nint context;

        /// <summary>Clear the back buffer to that colour and present it.</summary>
        public void Present((byte Red, byte Green, byte Blue) colour)
        {
            // Flip first, because that is the model PhotoBoost uses; the blt model where a driver
            // refuses flip on a child window, so the shape still has a swapchain to show.
            if (!Created(FlipDiscard, 2) && !Created(Discard, 1))
                throw new InvalidOperationException("no Direct3D 11 device and swapchain could be created, on the hardware or on WARP");

            var texture = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
            Check(Slot<GetBufferCall>(swap, 9)(swap, 0, ref texture, out var buffer), "IDXGISwapChain::GetBuffer");
            try
            {
                Check(Slot<CreateRenderTargetViewCall>(device, 9)(device, buffer, 0, out var view), "ID3D11Device::CreateRenderTargetView");
                try
                {
                    float[] rgba = [colour.Red / 255f, colour.Green / 255f, colour.Blue / 255f, 1f];
                    Slot<ClearRenderTargetViewCall>(context, 50)(context, view, rgba);
                    Check(Slot<PresentCall>(swap, 8)(swap, 0, 0), "IDXGISwapChain::Present");
                }
                finally
                {
                    Marshal.Release(view);
                }
            }
            finally
            {
                Marshal.Release(buffer);
            }
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            var child = CreateWindowExW(
                0, "static", "", ChildStyle, 0, 0, (int)(Width * scale), (int)(Height * scale), hwndParent.Handle, 0, 0, 0);

            return new HandleRef(this, child);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            foreach (var held in new[] { context, swap, device }.Where(one => one != 0))
                Marshal.Release(held);

            DestroyWindow(hwnd.Handle);
        }

        private bool Created(int effect, uint buffers)
        {
            var description = new SwapChainDescription
            {
                Format = B8G8R8A8,
                SampleCount = 1,
                BufferUsage = RenderTargetOutput,
                BufferCount = buffers,
                OutputWindow = Handle,
                Windowed = 1,
                SwapEffect = effect,
            };

            // The hardware first and WARP after it: a guest with no 3D acceleration has only WARP,
            // and the swapchain it presents is composed the same way.
            foreach (var driver in new[] { Hardware, Warp })
            {
                if (D3D11CreateDeviceAndSwapChain(0, driver, 0, 0, 0, 0, SdkVersion, ref description, out swap, out device, out _, out context) >= 0)
                    return true;
            }

            return false;
        }

        private static T Slot<T>(nint instance, int index)
            where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), index * nint.Size));

        private static void Check(int result, string call)
        {
            if (result < 0)
                throw new InvalidOperationException($"{call} failed with 0x{result:X8}");
        }

        private const int ChildStyle = 0x40000000 | 0x10000000;
        private const int Hardware = 1;
        private const int Warp = 5;
        private const uint SdkVersion = 7;
        private const int B8G8R8A8 = 87;
        private const uint RenderTargetOutput = 0x20;
        private const int Discard = 0;
        private const int FlipDiscard = 4;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetBufferCall(nint self, uint index, ref Guid riid, out nint surface);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateRenderTargetViewCall(nint self, nint resource, nint description, out nint view);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void ClearRenderTargetViewCall(nint self, nint view, [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] float[] colour);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PresentCall(nint self, uint interval, uint flags);

        /// <summary>DXGI_SWAP_CHAIN_DESC, flattened: the mode, the sampling, and the rest.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SwapChainDescription
        {
            public uint Width;
            public uint Height;
            public uint RefreshNumerator;
            public uint RefreshDenominator;
            public int Format;
            public int ScanlineOrdering;
            public int Scaling;
            public uint SampleCount;
            public uint SampleQuality;
            public uint BufferUsage;
            public uint BufferCount;
            public nint OutputWindow;
            public int Windowed;
            public int SwapEffect;
            public uint Flags;
        }

        [DllImport("d3d11.dll")]
        private static extern int D3D11CreateDeviceAndSwapChain(
            nint adapter,
            int driverType,
            nint software,
            uint flags,
            nint featureLevels,
            uint levels,
            uint sdkVersion,
            ref SwapChainDescription description,
            out nint swapChain,
            out nint device,
            out int level,
            out nint immediateContext);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern nint CreateWindowExW(
            int extendedStyle, string className, string title, int style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(nint window);
    }
}
