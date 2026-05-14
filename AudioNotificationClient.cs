using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SDS.Avalonia;

[GeneratedComClass]
internal sealed partial class AudioNotificationClient : IMMNotificationClient, IAudioEndpointVolumeCallback
{
    public Action<string>? DefaultDeviceChangedAction { get; set; }
    public Action<float, bool>? VolumeChangedAction { get; set; }

    public int OnDeviceStateChanged(string deviceId, uint newState) => 0;
    public int OnDeviceAdded(string deviceId) => 0;
    public int OnDeviceRemoved(string deviceId) => 0;
    public int OnPropertyValueChanged(string deviceId, in PropertyKey key) => 0;

    public int OnDefaultDeviceChanged(int dataFlow, int role, string defaultDeviceId)
    {
        if (dataFlow == 0 && role == 0) DefaultDeviceChangedAction?.Invoke(defaultDeviceId);
        return 0;
    }

    public int OnNotify(IntPtr notifyData)
    {
        if (notifyData == IntPtr.Zero) return 0;
        var data = Marshal.PtrToStructure<AudioVolumeNotificationData>(notifyData);
        VolumeChangedAction?.Invoke(data.masterVolume * 100f, data.muted != 0);
        return 0;
    }
}