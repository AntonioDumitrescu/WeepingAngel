using System.Runtime.InteropServices;
using OpenH264.Intermediaries;
using OpenH264.Interop;

namespace OpenH264;

public class WelsSvcEncoder : IDisposable
{
    private readonly IntPtr _nativePtr;
    private bool _isDisposed;

    #region Setup

    private static EncoderParamBaseDelegate? _initializeMethod;
    private static EncoderParamExtDelegate? _initializeExtMethod;
    private static EncoderParamExtDelegate? _getDefaultParamsMethod;
    private static VoidDelegate? _uninitializeMethod;
    private static SourcePictureFrameInfoDelegate? _encodeFrameMethod;
    private static FrameInfoDelegate? _encodeParameterSetsMethod;
    private static BoolIntDelegate? _forceIntraFrameMethod;

    private static void InitializeVTable(IntPtr encoderPtr)
    {
        if (_initializeMethod != null)
        {
            return;
        }

        var structure = Marshal.PtrToStructure<WelsSvcEncoderVTable>(Marshal.ReadIntPtr(encoderPtr, 0));
        _initializeMethod = Marshal.GetDelegateForFunctionPointer<EncoderParamBaseDelegate>(structure.Initialize);
        _initializeExtMethod = Marshal.GetDelegateForFunctionPointer<EncoderParamExtDelegate>(structure.InitializeExt);
        _getDefaultParamsMethod = Marshal.GetDelegateForFunctionPointer<EncoderParamExtDelegate>(structure.GetDefaultParams);
        _uninitializeMethod = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(structure.Uninitialize);
        _encodeFrameMethod = Marshal.GetDelegateForFunctionPointer<SourcePictureFrameInfoDelegate>(structure.EncodeFrame);
        _encodeParameterSetsMethod = Marshal.GetDelegateForFunctionPointer<FrameInfoDelegate>(structure.EncodeParameterSets);
        _forceIntraFrameMethod = Marshal.GetDelegateForFunctionPointer<BoolIntDelegate>(structure.ForceIntraFrame);
    }

    private struct WelsSvcEncoderVTable
    {
        public IntPtr Initialize;
        public IntPtr InitializeExt;
        public IntPtr GetDefaultParams;
        public IntPtr Uninitialize;
        public IntPtr EncodeFrame;
        public IntPtr EncodeParameterSets;
        public IntPtr ForceIntraFrame;
        public IntPtr SetOption;
        public IntPtr GetOption;
        public IntPtr Destructor;
    }

    #endregion

    public WelsSvcEncoder()
    {
        _nativePtr = EncoderInterop.WelsCreateSVCEncoder();
        InitializeVTable(_nativePtr);
    }

    public void Initialize(ref EncoderParamsBase param)
    {
        if (_initializeMethod!(_nativePtr, ref param) != 0)
        {
            throw new InvalidOperationException("Initialize failed");
        }
    }

    public void InitializeExt(ref EncoderParamsExt param)
    {
        if (_initializeExtMethod!(_nativePtr, ref param) != 0)
        {
            throw new InvalidOperationException("InitializeExt failed");
        }
    }

    public bool GetDefaultParams(ref EncoderParamsExt param) => _getDefaultParamsMethod!(_nativePtr, ref param) == 0;

    public bool EncodeFrame(ref SourcePicture source, ref FrameInfo frame) => _encodeFrameMethod!(_nativePtr, ref source, ref frame) == 0;

    public bool EncoderParameterSet(ref FrameInfo info) => _encodeParameterSetsMethod!(_nativePtr, ref info) == 0;

    public bool ForceIntraFrame(bool bIHdr, int layerId = -1) => _forceIntraFrameMethod!(_nativePtr, bIHdr, layerId) == 0;

    ~WelsSvcEncoder()
    {
        if (_isDisposed || _uninitializeMethod == null)
        {
            return;
        }

        _uninitializeMethod(_nativePtr);
        EncoderInterop.WelsDestroySVCEncoder(_nativePtr);
    }

    public void Dispose()
    {
        _isDisposed = true;
        if (_nativePtr == IntPtr.Zero || _uninitializeMethod == null)
        {
            return;
        }
        _uninitializeMethod(_nativePtr);
        EncoderInterop.WelsDestroySVCEncoder(_nativePtr);
        GC.SuppressFinalize(this);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EncoderParamBaseDelegate(IntPtr thisPtr, ref EncoderParamsBase param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EncoderParamExtDelegate(IntPtr thisPtr, ref EncoderParamsExt param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int VoidDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SourcePictureFrameInfoDelegate(
        IntPtr thisPtr,
        ref SourcePicture sourcePicture,
        ref FrameInfo frameInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FrameInfoDelegate(IntPtr thisPtr, ref FrameInfo info);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int BoolIntDelegate(IntPtr thisPtr, bool iHdr, int layerId);
}