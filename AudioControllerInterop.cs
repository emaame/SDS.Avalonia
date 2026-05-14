using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SDS.Avalonia;

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
    [PreserveSig] int RegisterEndpointNotificationCallback([MarshalUsing(typeof(ComInterfaceMarshaller<IMMNotificationClient>))] IMMNotificationClient client);
    [PreserveSig] int UnregisterEndpointNotificationCallback([MarshalUsing(typeof(ComInterfaceMarshaller<IMMNotificationClient>))] IMMNotificationClient client);
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
    [PreserveSig] int RegisterControlChangeNotify([MarshalUsing(typeof(ComInterfaceMarshaller<IAudioEndpointVolumeCallback>))] IAudioEndpointVolumeCallback client);
    [PreserveSig] int UnregisterControlChangeNotify([MarshalUsing(typeof(ComInterfaceMarshaller<IAudioEndpointVolumeCallback>))] IAudioEndpointVolumeCallback client);
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

[StructLayout(LayoutKind.Sequential)]
internal struct AudioVolumeNotificationData
{
    public Guid eventContext;
    public int muted;
    public float masterVolume;
    public uint channels;
    public float channelVolume1;
}

[GeneratedComInterface]
[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
internal partial interface IMMNotificationClient
{
    [PreserveSig] int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, uint newState);
    [PreserveSig] int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
    [PreserveSig] int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
    [PreserveSig] int OnDefaultDeviceChanged(int dataFlow, int role, [MarshalAs(UnmanagedType.LPWStr)] string defaultDeviceId);
    [PreserveSig] int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, in PropertyKey key);
}

[GeneratedComInterface]
[Guid("657804FA-D6AD-4496-8A60-352752AF4F89")]
internal partial interface IAudioEndpointVolumeCallback
{
    [PreserveSig] int OnNotify(IntPtr notifyData);
}
