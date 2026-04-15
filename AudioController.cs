using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SDS.Avalonia;

// デバイス情報を格納するレコード
public sealed record AudioDeviceInfo(string Id, string Name, string FriendlyName, bool IsDefaultDevice);

public static partial class AudioController
{
    static readonly Guid classIdMmDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    static readonly Guid interfaceIdMmDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    static readonly Guid classIdPolicyConfig = new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
    static readonly Guid interfaceIdPolicyConfig = new("F8679F50-850A-41CF-9C72-430F290290C8");
    static readonly Guid interfaceIdAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    private const int dataFlowRender = 0;
    private const int deviceStateActive = 1;
    private const uint storageModeRead = 0x00000000;

    public const uint CLSCTX_INPROC_SERVER = 1;
    public const uint CLSCTX_INPROC_HANDLER = 2;
    public const uint CLSCTX_LOCAL_SERVER = 4;
    public const uint CLSCTX_REMOTE_SERVER = 16;
    public const uint CLSCTX_ALL = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER;

    /// <summary>
    /// 有効な再生デバイスの一覧を取得します。
    /// </summary>
    public static IReadOnlyList<AudioDeviceInfo> GetPlaybackDevices()
    {
        var devices = new List<AudioDeviceInfo>();

        var resultCode = CoCreateInstance(
            in classIdMmDeviceEnumerator,
            IntPtr.Zero,
            1, // CLSCTX_INPROC_SERVER
            in interfaceIdMmDeviceEnumerator,
            out IMMDeviceEnumerator? enumerator);

        if (resultCode != 0 || enumerator is not IMMDeviceEnumerator validEnumerator) return [];

        // 現在のデフォルトデバイスIDを取得
        var defaultDeviceId = string.Empty;
        resultCode = validEnumerator.GetDefaultAudioEndpoint(dataFlowRender, 0, out var defaultEndpoint);
        if (resultCode == 0 && defaultEndpoint is IMMDevice validDefaultEndpoint)
        {
            resultCode = validDefaultEndpoint.GetId(out var id);
            if (resultCode == 0 && id is string validId)
            {
                defaultDeviceId = validId;
            }
        }

        resultCode = validEnumerator.EnumAudioEndpoints(dataFlowRender, deviceStateActive, out var collection);
        if (resultCode != 0 || collection is not IMMDeviceCollection validCollection) return [];

        resultCode = validCollection.GetCount(out var count);
        if (resultCode != 0) return [];

        var deviceDescriptionPropertyKey = new PropertyKey
        {
            formatId = new("a45c254e-df1c-4efd-8020-67d146a850e0"),
            propertyId = 2
        };

        var friendlyNamePropertyKey = new PropertyKey
        {
            formatId = new("a45c254e-df1c-4efd-8020-67d146a850e0"),
            propertyId = 14
        };

        for (var deviceIndex = 0u; deviceIndex < count; ++deviceIndex)
        {
            resultCode = validCollection.Item(deviceIndex, out var device);
            if (resultCode != 0 || device is not IMMDevice validDevice) continue;

            resultCode = validDevice.GetId(out var deviceId);
            if (resultCode != 0 || deviceId is not string validId) continue;

            resultCode = validDevice.OpenPropertyStore(storageModeRead, out var propertyStore);
            if (resultCode != 0 || propertyStore is not IPropertyStore validPropertyStore) continue;

            var name = GetPropertyValue(validPropertyStore, in deviceDescriptionPropertyKey);
            var friendlyName = GetPropertyValue(validPropertyStore, in friendlyNamePropertyKey);
            var isDefaultDevice = validId == defaultDeviceId;

            devices.Add(new(validId, name, friendlyName, isDefaultDevice));
        }

        return devices;
    }

    /// <summary>
    /// プロパティストアイインターフェースから指定したキーの文字列値を取得します。
    /// </summary>
    static string GetPropertyValue(IPropertyStore propertyStore, in PropertyKey propertyKey)
    {
        var resultCode = propertyStore.GetValue(in propertyKey, out var propertyVariant);
        var result = "Unknown";

        if (resultCode == 0 && propertyVariant.variantType == 31) // VT_LPWSTR
        {
            if (Marshal.PtrToStringUni(propertyVariant.pointerValue) is string validString)
            {
                result = validString;
            }
            PropVariantClear(ref propertyVariant);
        }

        return result;
    }

    /// <summary>
    /// 指定したデバイスIDをデフォルトのオーディオデバイスとして設定します。
    /// </summary>
    public static void SetDefaultDevice(string deviceId)
    {
        var resultCode = CoCreateInstance(
            in classIdPolicyConfig,
            IntPtr.Zero,
            CLSCTX_INPROC_SERVER, // CLSCTX_INPROC_SERVER
            in interfaceIdPolicyConfig,
            out IPolicyConfig? policyConfig);

        if (resultCode != 0 || policyConfig is not IPolicyConfig validPolicyConfig) return;

        // 0: eConsole, 1: eMultimedia, 2: eCommunications
        validPolicyConfig.SetDefaultEndpoint(deviceId, 0);
        validPolicyConfig.SetDefaultEndpoint(deviceId, 1);
        // validPolicyConfig.SetDefaultEndpoint(deviceId, 2);
    }

    /// <summary>
    /// 現在のデフォルトデバイスの音量 (0.0 ～ 100.0) を取得します。
    /// </summary>
    public static float GetDefaultDeviceVolume()
    {
        if (GetDefaultAudioEndpointVolume() is not IAudioEndpointVolume validVolume) return 0;

        var resultCode = validVolume.GetMasterVolumeLevelScalar(out var level);
        return resultCode == 0 ? level * 100 : 0f;
    }

    /// <summary>
    /// 現在のデフォルトデバイスの音量 (0.0 ～ 100.0) を設定します。
    /// </summary>
    public static void SetDefaultDeviceVolume(float volume)
    {
        if (GetDefaultAudioEndpointVolume() is not IAudioEndpointVolume validVolume) return;

        var level = float.Clamp(volume / 100f, 0f, 1f);
        var emptyContext = Guid.Empty;
        validVolume.SetMasterVolumeLevelScalar(level, in emptyContext);
    }

    /// <summary>
    /// 現在のデフォルトデバイスのミュート状態を取得します。
    /// </summary>
    public static bool GetDefaultDeviceIsMuted()
    {
        if (GetDefaultAudioEndpointVolume() is not IAudioEndpointVolume validVolume) return false;

        var resultCode = validVolume.GetMute(out var isMuted);
        return resultCode == 0 && isMuted != 0;
    }

    /// <summary>
    /// 現在のデフォルトデバイスのミュート状態を設定します。
    /// </summary>
    public static void SetDefaultDeviceIsMuted(bool isMuted)
    {
        if (GetDefaultAudioEndpointVolume() is not IAudioEndpointVolume validVolume) return;

        var emptyContext = Guid.Empty;
        validVolume.SetMute(isMuted ? 1 : 0, in emptyContext);
    }

    static IAudioEndpointVolume? GetDefaultAudioEndpointVolume()
    {
        var resultCode = CoCreateInstance(
            in classIdMmDeviceEnumerator,
            IntPtr.Zero,
            CLSCTX_INPROC_SERVER,
            in interfaceIdMmDeviceEnumerator,
            out IMMDeviceEnumerator? enumerator);

        if (resultCode != 0 || enumerator is not IMMDeviceEnumerator validEnumerator) return null;

        resultCode = validEnumerator.GetDefaultAudioEndpoint(dataFlowRender, 0, out var defaultEndpoint);
        if (resultCode != 0 || defaultEndpoint is not IMMDevice validEndpoint) return null;

        // nint でポインタを受け取る
        _ = validEndpoint.Activate(
            in interfaceIdAudioEndpointVolume,
            CLSCTX_ALL,
            IntPtr.Zero,
            out IAudioEndpointVolume? volumeEndpointPointer);

        return volumeEndpointPointer;
    }

    // --- P/Invoke Definitions ---

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(
        in Guid classId,
        IntPtr outerInstance,
        uint context,
        in Guid interfaceId,
        [MarshalUsing(typeof(ComInterfaceMarshaller<IMMDeviceEnumerator>))] out IMMDeviceEnumerator? result);

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(
        in Guid classId,
        IntPtr outerInstance,
        uint context,
        in Guid interfaceId,
        [MarshalUsing(typeof(ComInterfaceMarshaller<IPolicyConfig>))] out IPolicyConfig? result);

    [LibraryImport("ole32.dll")]
    private static partial int PropVariantClear(ref PropVariant propertyVariant);
}

// --- COM Interfaces & Structs ---

[StructLayout(LayoutKind.Sequential)]
public struct PropertyKey
{
    public Guid formatId;
    public uint propertyId;
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct PropVariant
{
    [FieldOffset(0)] public ushort variantType;
    [FieldOffset(2)] public ushort reserved1;
    [FieldOffset(4)] public ushort reserved2;
    [FieldOffset(6)] public ushort reserved3;
    [FieldOffset(8)] public IntPtr pointerValue;
}

[GeneratedComInterface]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
internal partial interface IMMDeviceEnumerator
{
    [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, [MarshalUsing(typeof(ComInterfaceMarshaller<IMMDeviceCollection>))] out IMMDeviceCollection? devices);

    // IntPtrのダミー定義から、実際のインターフェース取得用に修正
    [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, [MarshalUsing(typeof(ComInterfaceMarshaller<IMMDevice>))] out IMMDevice? endpoint);

    [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr device);
    [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
    [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
}

[GeneratedComInterface]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
internal partial interface IMMDeviceCollection
{
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int Item(uint index, [MarshalUsing(typeof(ComInterfaceMarshaller<IMMDevice>))] out IMMDevice? device);
}

[GeneratedComInterface]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
internal partial interface IMMDevice
{
    [PreserveSig] int Activate(in Guid interfaceId, uint context, IntPtr activationParams, out IAudioEndpointVolume? interfacePointer);
    [PreserveSig] int OpenPropertyStore(uint access, [MarshalUsing(typeof(ComInterfaceMarshaller<IPropertyStore>))] out IPropertyStore? properties);
    [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string? id);
    [PreserveSig] int GetState(out uint state);
}

[GeneratedComInterface]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
internal partial interface IPropertyStore
{
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int GetAt(uint index, out PropertyKey key);
    [PreserveSig] int GetValue(in PropertyKey key, out PropVariant value);
    [PreserveSig] int SetValue(in PropertyKey key, in PropVariant value);
    [PreserveSig] int Commit();
}

[GeneratedComInterface]
[Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
internal partial interface IPolicyConfig
{
    [PreserveSig] int GetMixFormat();
    [PreserveSig] int GetDeviceFormat();
    [PreserveSig] int ResetDeviceFormat();
    [PreserveSig] int SetDeviceFormat();
    [PreserveSig] int GetProcessingPeriod();
    [PreserveSig] int SetProcessingPeriod();
    [PreserveSig] int GetShareMode();
    [PreserveSig] int SetShareMode();
    [PreserveSig] int GetPropertyValue();
    [PreserveSig] int SetPropertyValue();

    // Windows 10/11 において VTable の 11 番目 (インデックス10)
    [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string endpointId, int role);

    [PreserveSig] int SetEndpointVisibility();
}

[GeneratedComInterface]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
internal partial interface IAudioEndpointVolume
{
    [PreserveSig] int RegisterControlChangeNotify(IntPtr client);
    [PreserveSig] int UnregisterControlChangeNotify(IntPtr client);
    [PreserveSig] int GetChannelCount(out uint channelCount);
    [PreserveSig] int SetMasterVolumeLevel(float levelDB, in Guid eventContext);
    [PreserveSig] int SetMasterVolumeLevelScalar(float level, in Guid eventContext);
    [PreserveSig] int GetMasterVolumeLevel(out float levelDB);
    [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
    [PreserveSig] int SetChannelVolumeLevel(uint channelNumber, float levelDB, in Guid eventContext);
    [PreserveSig] int SetChannelVolumeLevelScalar(uint channelNumber, float level, in Guid eventContext);
    [PreserveSig] int GetChannelVolumeLevel(uint channelNumber, out float levelDB);
    [PreserveSig] int GetChannelVolumeLevelScalar(uint channelNumber, out float level);
    [PreserveSig] int SetMute(int isMuted, in Guid eventContext);
    [PreserveSig] int GetMute(out int isMuted);
    [PreserveSig] int GetVolumeStepInfo(out uint step, out uint stepCount);
    [PreserveSig] int VolumeStepUp(in Guid eventContext);
    [PreserveSig] int VolumeStepDown(in Guid eventContext);
    [PreserveSig] int QueryHardwareSupport(out int pdwHardwareSupportMask);
    [PreserveSig] int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
}
