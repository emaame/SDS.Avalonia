using R3;
using System;
using System.Buffers;
using System.Text.RegularExpressions;

namespace SDS.Avalonia;

public sealed partial class AudioDeviceItem : IDisposable
{
    public BindableReactiveProperty<bool> IsBookmarked { get; }
    public BindableReactiveProperty<bool> IsEnabled { get; }

    public AudioDeviceInfo Device { get; }

    public ReactiveCommand<AudioDeviceItem?> SetDefaultDeviceCommand { get; }

    public string Name => GetDisplayName(Device);
    public string FriendlyName => Device.FriendlyName;

    readonly IDisposable disposable;

    public AudioDeviceItem(MainWindowViewModel viewModel, AudioDeviceInfo device)
    {
        SetDefaultDeviceCommand = viewModel.SetDefaultDeviceCommand;
        Device = device;
        IsBookmarked = new(false);
        IsEnabled = new(!device.IsDefaultDevice);

        disposable = Disposable.Combine(IsBookmarked, IsEnabled);
    }

    const string speakerName = "スピーカー";
    const string headsetName = "ヘッドセット";
    const string headphoneName = "ヘッドホン";

    static readonly Regex audioDeviceTypeRegex = CreateAudioDeviceTypeRegex();

    [GeneratedRegex("(スピーカー|ヘッドセット|ヘッドホン|ヘッドセット イヤフォン) \\((\\d+- )?(.+)\\)")]
    private static partial Regex CreateAudioDeviceTypeRegex();

    static readonly SearchValues<string> searchValues = SearchValues.Create(
        [speakerName, headsetName, headphoneName, "ヘッドセット イヤフォン"],
        StringComparison.OrdinalIgnoreCase);
    public static string GetDisplayName(AudioDeviceInfo device)
    {
        var (name, friendlyName) = (device.Name, device.FriendlyName);

        var span = name.AsSpan();
        var index = span.IndexOfAny(searchValues);

        var displayName = index >= 0
            ? audioDeviceTypeRegex.Replace(friendlyName, "$3")
            : name;
        return displayName;
    }

    public void Dispose() => disposable.Dispose();
}
