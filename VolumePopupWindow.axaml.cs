using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using R3;
using System;
using static SDS.Avalonia.WindowInterop;

namespace SDS.Avalonia;

public partial class VolumePopupWindow : Window
{
    public VolumePopupWindow()
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.Manual;
        // 座標計算完了前のフリッカー（意図しない位置への一瞬の描画）を防ぐ
        Opacity = 0;

        var viewModel = new VolumePopupViewModel();

        DataContext = viewModel;

        Deactivated += (_, _) =>
        {
            viewModel.Dispose();
            Close();
        };
    }

    protected override void OnOpened(EventArgs eventArgs)
    {
        base.OnOpened(eventArgs);
        AdjustPosition();
        Opacity = 1;
    }

    void AdjustPosition()
    {
        if (GetCursorPos(out PointInterop mousePoint) is false) return;

        var currentScreen = Screens.ScreenFromPoint(new(mousePoint.x, mousePoint.y));
        if (currentScreen is null) return;

        var workArea = currentScreen.WorkingArea;
        var scaling = currentScreen.Scaling;

        // ウィンドウの物理ピクセルサイズを計算 (DIPs * スケーリング)
        var pixelWidth = (int)(Bounds.Width * scaling);
        var pixelHeight = (int)(Bounds.Height * scaling);

        var (targetX, targetY) = CalculateWindowPositionSetCursorOverTheSliderThumb(mousePoint, pixelWidth, pixelHeight, scaling);

        // ワークエリア内に収まるようクランプ（タスクバーや画面外への重なりを完全に防止）
        targetX = int.Clamp(targetX, workArea.X, workArea.Right - pixelWidth);
        targetY = int.Clamp(targetY, workArea.Y, workArea.Bottom - pixelHeight);

        Position = new(targetX, targetY);
    }

#if false
    (int, int) CalculateWindowPosition(PixelRect workArea, PointInterop mousePoint, int pixelWidth, int pixelHeight, int margin)
    {
        // 基本はマウスカーソルの直上に配置
        var targetX = mousePoint.x - pixelWidth / 2;
        var targetY = mousePoint.y - pixelHeight - margin;

        // タスクバーが上部にある場合などでワークエリアを上回るなら、マウスの下に配置
        if (targetY < workArea.Y) targetY = mousePoint.y + margin;

        return (targetX, targetY);
    }
#endif

    // ウィンドウの中央をマウスポインタに合わせる
    static (int, int) CalculateWindowPositionSetCursorAsCenter(PointInterop mousePoint, int pixelWidth, int pixelHeight)
    {
        var targetX = mousePoint.x - pixelWidth / 2;
        var targetY = mousePoint.y - pixelHeight / 2;
        return (targetX, targetY);
    }

    // スライダのThumbをマウスポインタの直下に合わせる
    (int, int) CalculateWindowPositionSetCursorOverTheSliderThumb(PointInterop mousePoint, int pixelWidth, int pixelHeight, double scaling)
    {
        var volumeSlider = this.FindControl<Slider>("VolumeSlider");
        if (volumeSlider is null) return CalculateWindowPositionSetCursorAsCenter(mousePoint, pixelWidth, pixelHeight);

        // スライダ全体のウィンドウ内相対位置を取得 (DIPs)
        var presentationSource = volumeSlider.GetPresentationSource();
        if (presentationSource?.RootVisual is not Visual rootVisual) return CalculateWindowPositionSetCursorAsCenter(mousePoint, pixelWidth, pixelHeight);

        var sliderOffset = volumeSlider.TranslatePoint(new Point(0, 0), rootVisual);
        if (sliderOffset is not Point validSliderOffset) return CalculateWindowPositionSetCursorAsCenter(mousePoint, pixelWidth, pixelHeight);

        var range = volumeSlider.Maximum - volumeSlider.Minimum;
        var valueRatio = range > 0 ? (volumeSlider.Value - volumeSlider.Minimum) / range : 0.5;

        // 水平スライダの場合、幅に対する割合で Thumb の X座標 を算出
        var thumbRelativeX = volumeSlider.Bounds.Width * valueRatio;

        // Y座標はスライダコントロールの高さの中央とする
        var thumbRelativeY = volumeSlider.Bounds.Height / 2.0;

        // ウィンドウ全体における Thumb の絶対座標 (DIPs)
        var thumbAbsoluteX = validSliderOffset.X + thumbRelativeX;
        var thumbAbsoluteY = validSliderOffset.Y + thumbRelativeY;

        // 物理ピクセルサイズへの変換
        var pixelThumbX = (int)(thumbAbsoluteX * scaling);
        var pixelThumbY = (int)(thumbAbsoluteY * scaling);

        var targetX = mousePoint.x - pixelThumbX;
        var targetY = mousePoint.y - pixelThumbY;
        return (targetX, targetY);
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
