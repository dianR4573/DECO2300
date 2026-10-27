using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

/// <summary>
/// OpenXR feature that shows Meta Quest passthrough behind the app (XR_FB_passthrough).
/// It submits a passthrough layer under Unity's projection layer each frame and makes the
/// projection layer blend by alpha, so pixels the camera clears to alpha 0 show the real room.
/// </summary>
#if UNITY_EDITOR
[OpenXRFeature(
    UiName = "Meta Quest Passthrough (IP2b)",
    Desc = "Shows Quest passthrough behind transparent pixels for mixed reality table anchoring.",
    Company = "DECO2300",
    OpenxrExtensionStrings = ExtensionString,
    Version = "1.0.0",
    BuildTargetGroups = new[] { BuildTargetGroup.Android },
    FeatureId = FeatureId)]
#endif
public sealed class MetaQuestPassthroughFeature : OpenXRFeature
{
    public const string FeatureId = "com.deco2300.openxr.feature.passthrough";
    public const string ExtensionString = "XR_FB_passthrough";

    private const int TypePassthroughCreateInfo = 1000118001;
    private const int TypePassthroughLayerCreateInfo = 1000118002;
    private const int TypeCompositionLayerPassthrough = 1000118003;
    private const ulong IsRunningAtCreation = 0x1;
    private const ulong BlendTextureSourceAlpha = 0x2;
    private const int PurposeReconstruction = 0;
    private const int MaxLayers = 16;

    private static bool passthroughDesired = true;
    private static ulong sessionHandle;
    private static ulong passthroughHandle;
    private static ulong layerHandle;
    private static IntPtr passthroughLayerPtr;
    private static IntPtr combinedLayersPtr;

    [StructLayout(LayoutKind.Sequential)]
    private struct XrPassthroughCreateInfoFB { public int type; public IntPtr next; public ulong flags; }

    [StructLayout(LayoutKind.Sequential)]
    private struct XrPassthroughLayerCreateInfoFB { public int type; public IntPtr next; public ulong passthrough; public ulong flags; public int purpose; }

    [StructLayout(LayoutKind.Sequential)]
    private struct XrCompositionLayerPassthroughFB { public int type; public IntPtr next; public ulong flags; public ulong space; public ulong layerHandle; }

    // Byte offset of layerFlags in XrCompositionLayerBaseHeader { int type; void* next; ulong layerFlags; ulong space; }
    // on 64-bit (Quest builds are ARM64 only).
    private const int LayerFlagsOffset = 16;

    [StructLayout(LayoutKind.Sequential)]
    private struct XrFrameEndInfo { public int type; public IntPtr next; public long displayTime; public int environmentBlendMode; public uint layerCount; public IntPtr layers; }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetInstanceProcAddrFn(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string name, out IntPtr function);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EndFrameFn(ulong session, ref XrFrameEndInfo frameEndInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CreatePassthroughFn(ulong session, ref XrPassthroughCreateInfoFB createInfo, out ulong passthrough);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CreatePassthroughLayerFn(ulong session, ref XrPassthroughLayerCreateInfoFB createInfo, out ulong layer);

    // Start/pause/destroy for both the passthrough and its layer share this signature.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int HandleFn(ulong handle);

    // Delegates are kept in static fields so the GC never frees a pointer native code still holds.
    private static GetInstanceProcAddrFn nextGetInstanceProcAddr;
    private static GetInstanceProcAddrFn hookedGetInstanceProcAddr;
    private static EndFrameFn nextEndFrame;
    private static EndFrameFn hookedEndFrame;
    private static CreatePassthroughFn createPassthrough;
    private static CreatePassthroughLayerFn createPassthroughLayer;
    private static HandleFn passthroughStart, passthroughPause, destroyPassthrough;
    private static HandleFn layerResume, layerPause, destroyLayer;

    /// <summary>Show (true) or hide (false) the real room behind the app.</summary>
    public static void SetPassthroughActive(bool active)
    {
        passthroughDesired = active;
        ApplyState();
    }

    protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (func != IntPtr.Zero)
        {
            nextGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<GetInstanceProcAddrFn>(func);
            hookedGetInstanceProcAddr = HookedGetInstanceProcAddr;
            hookedEndFrame = HookedEndFrame;
            return Marshal.GetFunctionPointerForDelegate(hookedGetInstanceProcAddr);
        }
#endif
        return func;
    }

    protected override bool OnInstanceCreate(ulong xrInstance)
    {
        GetInstanceProcAddrFn getProc = nextGetInstanceProcAddr;
        if (getProc == null || xrInstance == 0)
        {
            return true;
        }

        createPassthrough = Resolve<CreatePassthroughFn>(getProc, xrInstance, "xrCreatePassthroughFB");
        createPassthroughLayer = Resolve<CreatePassthroughLayerFn>(getProc, xrInstance, "xrCreatePassthroughLayerFB");
        passthroughStart = Resolve<HandleFn>(getProc, xrInstance, "xrPassthroughStartFB");
        passthroughPause = Resolve<HandleFn>(getProc, xrInstance, "xrPassthroughPauseFB");
        destroyPassthrough = Resolve<HandleFn>(getProc, xrInstance, "xrDestroyPassthroughFB");
        layerResume = Resolve<HandleFn>(getProc, xrInstance, "xrPassthroughLayerResumeFB");
        layerPause = Resolve<HandleFn>(getProc, xrInstance, "xrPassthroughLayerPauseFB");
        destroyLayer = Resolve<HandleFn>(getProc, xrInstance, "xrDestroyPassthroughLayerFB");
        return true;
    }

    protected override void OnSessionCreate(ulong xrSession)
    {
        sessionHandle = xrSession;
        passthroughLayerPtr = Marshal.AllocHGlobal(Marshal.SizeOf<XrCompositionLayerPassthroughFB>());
        combinedLayersPtr = Marshal.AllocHGlobal(IntPtr.Size * MaxLayers);
        CreatePassthroughObjects();
        ApplyState();
    }

    protected override void OnSessionBegin(ulong xrSession)
    {
        ApplyState();
    }

    protected override void OnSessionDestroy(ulong xrSession)
    {
        if (layerHandle != 0) destroyLayer?.Invoke(layerHandle);
        if (passthroughHandle != 0) destroyPassthrough?.Invoke(passthroughHandle);
        layerHandle = 0;
        passthroughHandle = 0;
        sessionHandle = 0;
        Marshal.FreeHGlobal(passthroughLayerPtr);
        Marshal.FreeHGlobal(combinedLayersPtr);
        passthroughLayerPtr = IntPtr.Zero;
        combinedLayersPtr = IntPtr.Zero;
    }

    [AOT.MonoPInvokeCallback(typeof(GetInstanceProcAddrFn))]
    private static int HookedGetInstanceProcAddr(ulong instance, string name, out IntPtr function)
    {
        int result = nextGetInstanceProcAddr(instance, name, out function);
        if (result == 0 && name == "xrEndFrame" && function != IntPtr.Zero)
        {
            nextEndFrame = Marshal.GetDelegateForFunctionPointer<EndFrameFn>(function);
            function = Marshal.GetFunctionPointerForDelegate(hookedEndFrame);
        }

        return result;
    }

    // Runs on the render thread every frame: puts the passthrough layer first (underneath)
    // and tells the runtime to blend Unity's layers by their alpha.
    [AOT.MonoPInvokeCallback(typeof(EndFrameFn))]
    private static int HookedEndFrame(ulong session, ref XrFrameEndInfo frameEndInfo)
    {
        int count = (int)frameEndInfo.layerCount;
        if (passthroughDesired && layerHandle != 0 && combinedLayersPtr != IntPtr.Zero &&
            count > 0 && count < MaxLayers && frameEndInfo.layers != IntPtr.Zero)
        {
            // The passthrough layer struct is written once in CreatePassthroughObjects.
            Marshal.WriteIntPtr(combinedLayersPtr, 0, passthroughLayerPtr);

            for (int i = 0; i < count; i++)
            {
                IntPtr layer = Marshal.ReadIntPtr(frameEndInfo.layers, i * IntPtr.Size);
                if (layer != IntPtr.Zero)
                {
                    long flags = Marshal.ReadInt64(layer, LayerFlagsOffset);
                    Marshal.WriteInt64(layer, LayerFlagsOffset, flags | (long)BlendTextureSourceAlpha);
                }

                Marshal.WriteIntPtr(combinedLayersPtr, (i + 1) * IntPtr.Size, layer);
            }

            frameEndInfo.layers = combinedLayersPtr;
            frameEndInfo.layerCount = (uint)(count + 1);
        }

        return nextEndFrame(session, ref frameEndInfo);
    }

    private static T Resolve<T>(GetInstanceProcAddrFn getProc, ulong instance, string name) where T : class
    {
        return getProc(instance, name, out IntPtr ptr) == 0 && ptr != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(ptr) : null;
    }

    private static void CreatePassthroughObjects()
    {
        if (createPassthrough == null || createPassthroughLayer == null)
        {
            return;
        }

        XrPassthroughCreateInfoFB info = new XrPassthroughCreateInfoFB { type = TypePassthroughCreateInfo, flags = IsRunningAtCreation };
        int result = createPassthrough(sessionHandle, ref info, out passthroughHandle);
        if (result < 0)
        {
            Debug.LogWarning("[NotionAR] xrCreatePassthroughFB returned " + result);
            passthroughHandle = 0;
            return;
        }

        XrPassthroughLayerCreateInfoFB layerInfo = new XrPassthroughLayerCreateInfoFB
        {
            type = TypePassthroughLayerCreateInfo,
            passthrough = passthroughHandle,
            flags = IsRunningAtCreation,
            purpose = PurposeReconstruction
        };
        result = createPassthroughLayer(sessionHandle, ref layerInfo, out layerHandle);
        if (result < 0)
        {
            Debug.LogWarning("[NotionAR] xrCreatePassthroughLayerFB returned " + result);
            layerHandle = 0;
            return;
        }

        Marshal.StructureToPtr(new XrCompositionLayerPassthroughFB
        {
            type = TypeCompositionLayerPassthrough,
            flags = BlendTextureSourceAlpha,
            layerHandle = layerHandle
        }, passthroughLayerPtr, false);
    }

    private static void ApplyState()
    {
        if (sessionHandle == 0)
        {
            return;
        }

        // Quest only supports OPAQUE; passthrough comes from the XR_FB_passthrough layer, not the
        // blend mode. Requesting AlphaBlend makes every xrEndFrame fail and nothing is displayed.
        SetEnvironmentBlendMode(XrEnvironmentBlendMode.Opaque);

        if (passthroughHandle == 0 || layerHandle == 0)
        {
            return;
        }

        if (passthroughDesired)
        {
            passthroughStart?.Invoke(passthroughHandle);
            layerResume?.Invoke(layerHandle);
        }
        else
        {
            layerPause?.Invoke(layerHandle);
            passthroughPause?.Invoke(passthroughHandle);
        }
    }
}
