using System.Runtime.InteropServices;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Merlin.Gui
{
    public class VeldridImGuiWindow : IDisposable
    {
        private readonly GCHandle _gcHandle;
        private readonly GraphicsDevice _graphicsDevice;

        public Sdl2Window Window { get; }

        public Swapchain SwapChain { get; }

        public VeldridImGuiWindow(GraphicsDevice graphicsDevice, ImGuiViewportPtr viewport)
        {
            _gcHandle = GCHandle.Alloc(this);
            _graphicsDevice = graphicsDevice;
            var vp1 = viewport;

            var flags = SDL_WindowFlags.Hidden;

            if ((viewport.Flags & ImGuiViewportFlags.NoTaskBarIcon) != 0)
            {
                flags |= SDL_WindowFlags.SkipTaskbar;
            }

            if ((viewport.Flags & ImGuiViewportFlags.NoDecoration) != 0)
            {
                flags |= SDL_WindowFlags.Borderless;
            }
            else
            {
                flags |= SDL_WindowFlags.Resizable;
            }

            if ((viewport.Flags & ImGuiViewportFlags.TopMost) != 0)
            {
                flags |= SDL_WindowFlags.AlwaysOnTop;
            }

            Window = new Sdl2Window(
                "No Title Yet",
                (int)viewport.Pos.X, (int)viewport.Pos.Y,
                (int)viewport.Size.X, (int)viewport.Size.Y,
                flags,
                false);
            Window.Resized += () => vp1.PlatformRequestResize = true;
            Window.Moved += p => vp1.PlatformRequestMove = true;
            Window.Closed += () => vp1.PlatformRequestClose = true;

            var scSource = VeldridStartup.GetSwapchainSource(Window);
            var scDesc = new SwapchainDescription(scSource, (uint)Window.Width, (uint)Window.Height, null, true, false);
            SwapChain = _graphicsDevice.ResourceFactory.CreateSwapchain(scDesc);
            Window.Resized += () => SwapChain.Resize((uint)Window.Width, (uint)Window.Height);

            viewport.PlatformUserData = (IntPtr)_gcHandle;
        }

        public VeldridImGuiWindow(GraphicsDevice graphicsDevice, ImGuiViewportPtr viewport, Sdl2Window window)
        {
            _gcHandle = GCHandle.Alloc(this);
            _graphicsDevice = graphicsDevice;
            Window = window;
            viewport.PlatformUserData = (IntPtr)_gcHandle;
        }

        public void Update()
        {
            Window.PumpEvents();
        }

        public void Dispose()
        {
            _graphicsDevice.WaitForIdle(); // TODO: Shouldn't be necessary, but Vulkan backend trips a validation error (swapchain in use when disposed).
            SwapChain.Dispose();
            Window.Close();
            _gcHandle.Free();
        }
    }
}
