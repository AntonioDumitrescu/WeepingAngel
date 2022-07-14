using System.Runtime.InteropServices;
using OpenH264.Intermediaries;
using OpenH264.Interop;

namespace OpenH264;

public sealed class WelsDecoder : IDisposable
{
    private readonly IntPtr _nativePtr;
    private bool _isDisposed;

    #region Setup

    private static DecoderParamDelegate? _initializeMethod;
    private static UninitializeDelegate? _uninitializeMethod;
    private static DecodeFrameDelegate? _decodeFrameMethod;
    private static DecodeFrame2Delegate? _decodeFrame2Method;

    private static void InitializeVTable(IntPtr decoderPtr)
    {
        if (_initializeMethod != null)
        {
            return;
        }

        var structure = Marshal.PtrToStructure<WelsSvcDecoderVTable>(Marshal.ReadIntPtr(decoderPtr, 0));
        _initializeMethod = Marshal.GetDelegateForFunctionPointer<DecoderParamDelegate>(structure.Initialize);
        _uninitializeMethod = Marshal.GetDelegateForFunctionPointer<UninitializeDelegate>(structure.Uninitialize);
        _decodeFrameMethod = Marshal.GetDelegateForFunctionPointer<DecodeFrameDelegate>(structure.DecodeFrame);
        _decodeFrame2Method = Marshal.GetDelegateForFunctionPointer<DecodeFrame2Delegate>(structure.DecodeFrame2);
    }

    private struct WelsSvcDecoderVTable
    {
        public IntPtr Initialize;
        public IntPtr Uninitialize;
        public IntPtr DecodeFrame;
        public IntPtr DecodeFrameNoDelay;
        public IntPtr DecodeFrame2;
        public IntPtr FlushFrame;
        public IntPtr DecodeParser;
        public IntPtr DecodeFrameEx;
        public IntPtr SetOption;
        public IntPtr GetOption;
    }

    #endregion

    public WelsDecoder()
    {
        _nativePtr = DecoderInterop.WelsCreateDecoder();
        InitializeVTable(_nativePtr);
    }

    public void Initialize(ref DecodingParameters param)
    {
        if (_initializeMethod!(_nativePtr, ref param) != 0)
        {
            throw new InvalidOperationException("Initialize failed");
        }
    }

    public DecodingState DecodeFrame(
        IntPtr source,
        int sourceLength, 
        IntPtr destination, 
        IntPtr stride, 
        int width,
        int height
    ) => _decodeFrameMethod!(_nativePtr, source, sourceLength, destination, stride, width, height);

    public DecodingState DecodeFrame2(IntPtr source, int sourceLength, IntPtr destination, ref BufferInfo info) =>
        _decodeFrame2Method!(_nativePtr, source, sourceLength, destination, ref info);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DecoderParamDelegate(IntPtr thisPtr, ref DecodingParameters param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int UninitializeDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate DecodingState DecodeFrameDelegate(
        IntPtr thisPtr, 
        IntPtr src, 
        int srcLen, 
        IntPtr dst,
        IntPtr stride, 
        int width,
        int height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate DecodingState DecodeFrame2Delegate(IntPtr thisPtr, IntPtr src, int srcLen, IntPtr dst,
        ref BufferInfo bufferInfo);

    ~WelsDecoder()
    {
        if (_isDisposed || _uninitializeMethod == null)
        {
            return;
        }

        _uninitializeMethod(_nativePtr);
        DecoderInterop.WelsDestroyDecoder(_nativePtr);
    }

    public void Dispose()
    {
        _isDisposed = true;
        if (_nativePtr == IntPtr.Zero || _uninitializeMethod == null)
        {
            return;
        }
        _uninitializeMethod(_nativePtr);
        DecoderInterop.WelsDestroyDecoder(_nativePtr);
        GC.SuppressFinalize(this);
    }
}