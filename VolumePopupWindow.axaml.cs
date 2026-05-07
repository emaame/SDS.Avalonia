using Avalonia.Controls;
using R3;
using System;

namespace SDS.Avalonia;

public partial class VolumePopupWindow : Window
{
    public VolumePopupWindow()
    {
        InitializeComponent();
        
        var volume = new BindableReactiveProperty<float>(AudioController.GetDefaultDeviceVolume());
        
        // 音量変更を同期
        volume.Skip(1).Subscribe(static v => AudioController.SetDefaultDeviceVolume(v));
        
        DataContext = new VolumePopupViewModel(volume);

        Deactivated += (_, _) => Close();
    }
}

public sealed record VolumePopupViewModel(BindableReactiveProperty<float> Volume);
