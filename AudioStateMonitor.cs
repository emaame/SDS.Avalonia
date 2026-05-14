using R3;
using System;
using static SDS.Avalonia.AudioController;

namespace SDS.Avalonia;

public sealed class AudioStateMonitor : IDisposable
{
    readonly Subject<string> defaultDeviceChangedSubject;
    readonly Subject<(float Volume, bool IsMuted)> volumeChangedSubject;
    readonly IDisposable disposable;

    IMMDeviceEnumerator? enumerator;
    IAudioEndpointVolume? currentVolumeEndpoint;
    readonly AudioNotificationClient notificationClient;

    public Observable<string> DefaultDeviceChanged => defaultDeviceChangedSubject;
    public Observable<(float Volume, bool IsMuted)> VolumeChanged => volumeChangedSubject;

    public AudioStateMonitor()
    {
        var builder = new DisposableBuilder();

        defaultDeviceChangedSubject = new Subject<string>().AddTo(ref builder);
        volumeChangedSubject = new Subject<(float, bool)>().AddTo(ref builder);

        notificationClient = new()
        {
            DefaultDeviceChangedAction = OnDefaultDeviceChanged,
            VolumeChangedAction = OnVolumeChanged
        };

        InitializeCom();

        disposable = builder.Build();
    }

    void InitializeCom()
    {
        var resultCode = CoCreateInstance(in classIdMmDeviceEnumerator, IntPtr.Zero, CLSCTX_INPROC_SERVER, in interfaceIdMmDeviceEnumerator, out enumerator);
        if (resultCode != 0 || enumerator is not IMMDeviceEnumerator validEnumerator) return;

        validEnumerator.RegisterEndpointNotificationCallback(notificationClient);
        SubscribeToCurrentVolumeEndpoint(validEnumerator);
    }

    void SubscribeToCurrentVolumeEndpoint(IMMDeviceEnumerator validEnumerator)
    {
        if (currentVolumeEndpoint is IAudioEndpointVolume oldVolume) oldVolume.UnregisterControlChangeNotify(notificationClient);
        currentVolumeEndpoint = null;

        var resultCode = validEnumerator.GetDefaultAudioEndpoint(dataFlowRender, 0, out var endpoint);
        if (resultCode != 0 || endpoint is not IMMDevice validEndpoint) return;

        resultCode = validEndpoint.Activate(in interfaceIdAudioEndpointVolume, CLSCTX_ALL, IntPtr.Zero, out var volumeEndpoint);
        if (resultCode != 0 || volumeEndpoint is not IAudioEndpointVolume validVolume) return;

        currentVolumeEndpoint = validVolume;
        currentVolumeEndpoint.RegisterControlChangeNotify(notificationClient);
    }

    void OnDefaultDeviceChanged(string deviceId)
    {
        if (enumerator is IMMDeviceEnumerator validEnumerator) SubscribeToCurrentVolumeEndpoint(validEnumerator);
        defaultDeviceChangedSubject.OnNext(deviceId);
    }

    void OnVolumeChanged(float volume, bool isMuted) => volumeChangedSubject.OnNext((volume, isMuted));

    public void Dispose()
    {
        if (enumerator is IMMDeviceEnumerator validEnumerator) validEnumerator.UnregisterEndpointNotificationCallback(notificationClient);
        if (currentVolumeEndpoint is IAudioEndpointVolume validVolume) validVolume.UnregisterControlChangeNotify(notificationClient);
        disposable.Dispose();
    }
}