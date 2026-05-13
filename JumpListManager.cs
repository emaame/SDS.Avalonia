using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ZLinq;

namespace SDS.Avalonia;

public static class JumpListManager
{
    public const string AppId = "SDS.Avalonia.App";

    public static void Initialize()
    {
        try
        {
            JumpListInterop.SetCurrentProcessExplicitAppUserModelID(AppId);
        }
        catch
        {
            // Ignore if failed (e.g. non-Windows)
        }
    }
    public static unsafe void UpdateJumpList(IEnumerable<AudioDeviceInfo> bookmarks)
    {
        var exePath = Environment.ProcessPath;
        if (exePath is null) return;

        var createResultCode = JumpListInterop.CoCreateInstance(
            in JumpListInterop.ClsidDestinationList,
            IntPtr.Zero,
            1, // CLSCTX_INPROC_SERVER
            in JumpListInterop.IidCustomDestinationList,
            out ICustomDestinationList? destinationList);
        if (createResultCode != 0 || destinationList is null) return; // ログ出力等は要件に応じて追加してください

        destinationList.SetAppID(AppId);

        var beginListCode = destinationList.BeginList(out var _, in JumpListInterop.IidObjectArray, out var _);
        if (beginListCode != 0) return;

        if (JumpListInterop.CoCreateInstance(in JumpListInterop.ClsidEnumerableObjectCollection, IntPtr.Zero, 1, in JumpListInterop.IidObjectCollection, out IObjectCollection? tasksCollection) == 0 && tasksCollection is not null)
        {
            if (CreateShellLink(exePath, "音量調整", "--volume-popup", out var volumeLink) == 0 && volumeLink is not null)
            {
                void* unmanagedLink = ComInterfaceMarshaller<IShellLinkW>.ConvertToUnmanaged(volumeLink);
                tasksCollection.AddObject((nint)unmanagedLink);
                Marshal.Release((nint)unmanagedLink);
            }

            destinationList.AddUserTasks(tasksCollection);
        }

        if (JumpListInterop.CoCreateInstance(in JumpListInterop.ClsidEnumerableObjectCollection, IntPtr.Zero, 1, in JumpListInterop.IidObjectCollection, out IObjectCollection? bookmarksCollection) == 0 && bookmarksCollection is not null)
        {
            foreach (var device in bookmarks.AsValueEnumerable().Take(10))
            {
                if (CreateShellLink(exePath, device.FriendlyName, $"--switch-device {device.Id}", out var deviceLink) == 0 && deviceLink is not null)
                {
                    void* unmanagedLink = ComInterfaceMarshaller<IShellLinkW>.ConvertToUnmanaged(deviceLink);
                    bookmarksCollection.AddObject((nint)unmanagedLink);
                    Marshal.Release((nint)unmanagedLink);
                }
            }

            destinationList.AppendCategory("ブックマーク", bookmarksCollection);
        }

        destinationList.CommitList();
    }

    static int CreateShellLink(string executablePath, string itemTitle, string commandArguments, out IShellLinkW? shellLink)
    {
        var createResultCode = JumpListInterop.CoCreateInstance(
            in JumpListInterop.ClsidShellLink,
            IntPtr.Zero,
            1,
            in JumpListInterop.IidShellLink,
            out shellLink);
        if (createResultCode != 0 || shellLink is null) return createResultCode;

        shellLink.SetPath(executablePath);
        shellLink.SetArguments(commandArguments);

        // E_INVALIDARGを回避するため、IPropertyStore経由で必ずPKEY_Titleを設定する
        if (shellLink is IPropertyStore propertyStore)
        {
            // PKEY_Title = {F29F85E0-4FF9-1068-AB91-08002B27B3D9}, 2
            PropertyKey titleKey = new() { formatId = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), propertyId = 2 };
            PropVariant titleVariant = new() { variantType = 31 /* VT_LPWSTR */, pointerValue = Marshal.StringToCoTaskMemUni(itemTitle) };

            propertyStore.SetValue(in titleKey, in titleVariant);
            propertyStore.Commit();

            Marshal.FreeCoTaskMem(titleVariant.pointerValue);
        }

        return 0;
    }
}
