using System.Runtime.InteropServices;
using System.Text;

namespace Merlin.Gui;

public class NullTerminatedString : IDisposable
{
    public IntPtr DataPtr { get; }

    public unsafe NullTerminatedString(string str)
    {
        var byteCount = Encoding.ASCII.GetByteCount(str);

        DataPtr = Marshal.AllocHGlobal(byteCount + 1);
      
        fixed (char* stringPointer = str)
        {
            var end = Encoding.ASCII.GetBytes(stringPointer, str.Length, (byte*)DataPtr, byteCount);
            ((byte*)DataPtr)[end] = 0;
        }
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(DataPtr);
    }
}