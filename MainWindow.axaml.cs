using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using System;
using System.Runtime.InteropServices;

namespace SDS.Avalonia;

readonly record struct RECT(int Left, int Top, int Right, int Bottom);

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (DataContext is not MainWindowViewModel viewModel ||
            GetTopLevel(this)?.TryGetPlatformHandle()?.Handle is not nint hWnd) return;

        var x = viewModel.Left.Value;
        var y = viewModel.Top.Value;
        var width = int.Max(500, viewModel.Width.Value);
        var height = int.Max(500, viewModel.Height.Value);

        SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, 0);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        this.WindowState = WindowState.Normal;

        if (DataContext is not MainWindowViewModel viewModel ||
            GetTopLevel(this)?.TryGetPlatformHandle()?.Handle is not nint hWnd ||
            !GetWindowRect(hWnd, out var rect)) return;

        viewModel.Left.Value = rect.Left;
        viewModel.Top.Value = rect.Top;
        viewModel.Width.Value = rect.Right - rect.Left;
        viewModel.Height.Value = rect.Bottom - rect.Top;

        viewModel.Save();
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}
