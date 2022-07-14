using Veldrid;

namespace Yggdrasil.Api.Server;

public interface IServerWindow
{
    GraphicsDevice GraphicsDevice { get; }

    IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture);
}