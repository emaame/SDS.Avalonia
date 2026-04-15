using ObservableCollections;
using R3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ZLinq;

namespace SDS.Avalonia;

public partial record Config(
    bool IsFilterBookmarked,
    string[] BookmarkedDeviceNames,
    int Left,
    int Top,
    int Width,
    int Height);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Config))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext { }

public sealed class MainWindowViewModel : IDisposable
{
    public NotifyCollectionChangedSynchronizedViewList<AudioDeviceItem> AudioDeviceItems { get; }
    public ObservableList<AudioDeviceItem> Items { get; }
    ISynchronizedView<AudioDeviceItem, AudioDeviceItem> ItemsView { get; }

    public BindableReactiveProperty<bool> IsMuted { get; }
    public BindableReactiveProperty<float> Volume { get; }

    public BindableReactiveProperty<bool> IsFilterBookmarked { get; }
    public BindableReactiveProperty<int> ItemWidth { get; }
    public BindableReactiveProperty<int> ItemHeight { get; }

    public BindableReactiveProperty<int> Left { get; }
    public BindableReactiveProperty<int> Top { get; }
    public BindableReactiveProperty<int> Width { get; }
    public BindableReactiveProperty<int> Height { get; }

    public ReactiveCommand<AudioDeviceItem?> SetDefaultDeviceCommand { get; }
    public ReactiveCommand ToggleIsMutedCommand { get; }
    public ReactiveCommand ResetDevicesCommand { get; }

    readonly IDisposable disposables;
    readonly DisposableBag devicesDisposables;

    readonly Lock lockDevices = new();

    public MainWindowViewModel()
    {
        var builder = new DisposableBuilder();

        Items = [];

        ItemsView = Items.CreateView(static b => b).AddTo(ref builder);

        IsFilterBookmarked = new BindableReactiveProperty<bool>(false).AddTo(ref builder);

        Left = new BindableReactiveProperty<int>(600).AddTo(ref builder);
        Top = new BindableReactiveProperty<int>(600).AddTo(ref builder);
        Width = new BindableReactiveProperty<int>(1900).AddTo(ref builder);
        Height = new BindableReactiveProperty<int>(800).AddTo(ref builder);

        ItemWidth = new BindableReactiveProperty<int>(600);
        ItemHeight = new BindableReactiveProperty<int>(600);

        AudioDeviceItems = ItemsView.ToNotifyCollectionChanged().AddTo(ref builder);

        Volume = new BindableReactiveProperty<float>(AudioController.GetDefaultDeviceVolume()).AddTo(ref builder);
        IsMuted = new BindableReactiveProperty<bool>(AudioController.GetDefaultDeviceIsMuted()).AddTo(ref builder);

        SetDefaultDeviceCommand = new ReactiveCommand<AudioDeviceItem?>(SetDefaultDevice).AddTo(ref builder);
        ToggleIsMutedCommand = new ReactiveCommand(ToggleIsMuted).AddTo(ref builder);
        ResetDevicesCommand = new ReactiveCommand(ResetDevices).AddTo(ref builder);

        IsFilterBookmarked.Subscribe(IsFilterBookmarkedChanged).AddTo(ref builder);
        Volume.Skip(1).Subscribe(SetVolume).AddTo(ref builder);
        IsMuted.Skip(1).Subscribe(SetIsMuted).AddTo(ref builder);

        disposables = builder.Build();

        ResetDevices(Unit.Default);
        Load();
    }

    void ToggleIsMuted(Unit _) => IsMuted.Value = !IsMuted.Value;
    void SyncToDefaultPlaybackDevice()
    {
        Volume.Value = AudioController.GetDefaultDeviceVolume();
        IsMuted.Value = AudioController.GetDefaultDeviceIsMuted();
    }

    public static void SetVolume(float volume) => AudioController.SetDefaultDeviceVolume(volume);
    public static void SetIsMuted(bool isMuted) => AudioController.SetDefaultDeviceIsMuted(isMuted);

    public void SetDefaultDevice(AudioDeviceItem? target)
    {
        if (target is not AudioDeviceItem audioDeviceItem) return;

        AudioController.SetDefaultDevice(audioDeviceItem.Device.Id);

        lock (lockDevices)
        {
            foreach (var item in Items)
            {
                if (item is not AudioDeviceItem audioDevice) continue;
                audioDevice.IsEnabled.Value = true;
            }
        }
        audioDeviceItem.IsEnabled.Value = false;

        SyncToDefaultPlaybackDevice();
    }

    public void IsFilterBookmarkedChanged(bool isFilterBookmarked)
    {
        static bool Filter(AudioDeviceItem? item) =>
            item is not AudioDeviceItem audioDeviceItem ||
            audioDeviceItem.IsBookmarked.Value;

        if (isFilterBookmarked) ItemsView.AttachFilter(Filter);
        else ItemsView.ResetFilter();
    }

    public void ResetDevices(Unit _)
    {
        var playbackDevices = AudioController.GetPlaybackDevices();
        lock (lockDevices)
        {
            var bookmarkedDeviceNames = GetBookmarkedDeviceNames();

            devicesDisposables.Clear();
            Items.Clear();
            Items.AddRange([.. playbackDevices.AsValueEnumerable().Select(device => new AudioDeviceItem(this, device))]);
            SetBookmarked(Items, bookmarkedDeviceNames);

            Items.ForEach(device => devicesDisposables.Add(device));
        }
    }

    static string GetConfigPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            nameof(SDS),
            "config.json"
        );

    public void Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return;

        var jsonText = File.ReadAllText(path, System.Text.Encoding.UTF8);

        if (JsonSerializer.Deserialize(jsonText, JsonSourceGenerationContext.Default.Config) is not Config config) return;

        AudioDeviceItem?[] items;
        lock (lockDevices) { items = [.. Items]; }
        SetBookmarked(items, config.BookmarkedDeviceNames);

        Left.Value = config.Left;
        Top.Value = config.Top;
        Width.Value = config.Width;
        Height.Value = config.Height;
        IsFilterBookmarked.Value = config.IsFilterBookmarked;
    }
    public void Save()
    {
        var bookmarkedDeviceNames = GetBookmarkedDeviceNames();

        var config = new Config(
            IsFilterBookmarked.Value,
            bookmarkedDeviceNames,
            Left.Value, Top.Value,
            Width.Value, Height.Value);

        var path = GetConfigPath();
        CreateDirectorySafe(Path.GetDirectoryName(path));
        var jsonText = JsonSerializer.Serialize(config, JsonSourceGenerationContext.Default.Config);
        File.WriteAllText(path, jsonText, System.Text.Encoding.UTF8);
    }

    static void SetBookmarked(IEnumerable<AudioDeviceItem?> items, string[] bookmarkedDeviceNames)
    {
        var hashSet = items
            .OfType<AudioDeviceItem>()
            .ToDictionary(static item => item.Device.FriendlyName, static item => item);

        foreach (var name in bookmarkedDeviceNames)
        {
            if (!hashSet.TryGetValue(name, out var item)) continue;
            item.IsBookmarked.Value = true;
        }
    }

    string[] GetBookmarkedDeviceNames()
    {
        AudioDeviceItem?[] items;
        lock (lockDevices) { items = [.. Items]; }
        return [.. items
            .OfType<AudioDeviceItem>()
            .Where(static item => item.IsBookmarked.Value)
            .Select(static item => item.Device.FriendlyName)];
    }

    static void CreateDirectorySafe(string? path)
    {
        if (path is not { }) return;
        try { Directory.CreateDirectory(path); }
        catch (Exception ex)
        {
            Debug.WriteLine("ディレクトリ作成中にエラーが発生しました: " + ex.Message);
        }
    }

    public void Dispose() => disposables.Dispose();
}
