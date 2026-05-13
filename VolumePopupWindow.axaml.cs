using Avalonia.Controls;
using R3;
using System;

namespace SDS.Avalonia;

public partial class VolumePopupWindow : Window
{
    public VolumePopupWindow()
    {
        InitializeComponent();

        var viewModel = new VolumePopupViewModel();

        DataContext = viewModel;

        Deactivated += (_, _) =>
        {
            viewModel.Dispose();
            Close();
        };
    }
}

public sealed record VolumePopupViewModel : IDisposable
{
    public BindableReactiveProperty<float> Volume { get; }

    readonly IDisposable disposable;

    public VolumePopupViewModel()
    {
        var builder = new DisposableBuilder();

        Volume = new BindableReactiveProperty<float>(AudioController.GetDefaultDeviceVolume()).AddTo(ref builder);
        Volume.Skip(1).Subscribe(static v => AudioController.SetDefaultDeviceVolume(v)).AddTo(ref builder);

        disposable = builder.Build();
    }
    public void Dispose() => disposable.Dispose();
}
