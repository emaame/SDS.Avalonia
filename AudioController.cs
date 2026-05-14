using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SDS.Avalonia;

// デバイス情報を格納するレコード
public sealed record AudioDeviceInfo(string Id, string Name, string FriendlyName, bool IsDefaultDevice);

public static partial class AudioController
{
    internal static readonly Guid classIdMmDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    internal static readonly Guid interfaceIdMmDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    internal static readonly Guid classIdPolicyConfig = new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
    internal static readonly Guid interfaceIdPolicyConfig = new("F8679F50-850A-41CF-9C72-430F290290C8");
    internal static readonly Guid interfaceIdAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    internal const int dataFlowRender = 0;
    internal const int deviceStateActive = 1;
    internal const uint storageModeRead = 0x00000000;

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
    internal static partial int CoCreateInstance(
        in Guid classId,
        IntPtr outerInstance,
        uint context,
        in Guid interfaceId,
        [MarshalUsing(typeof(ComInterfaceMarshaller<IMMDeviceEnumerator>))] out IMMDeviceEnumerator? result);

    [LibraryImport("ole32.dll")]
    internal static partial int CoCreateInstance(
        in Guid classId,
        IntPtr outerInstance,
        uint context,
        in Guid interfaceId,
        [MarshalUsing(typeof(ComInterfaceMarshaller<IPolicyConfig>))] out IPolicyConfig? result);

    [LibraryImport("ole32.dll")]
    internal static partial int PropVariantClear(ref PropVariant propertyVariant);
}
