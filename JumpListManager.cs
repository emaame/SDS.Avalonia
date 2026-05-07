using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

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

        var resultCode = JumpListInterop.CoCreateInstance(
            in JumpListInterop.ClsidDestinationList,
            IntPtr.Zero,
            1, // CLSCTX_INPROC_SERVER
            in JumpListInterop.ClsidDestinationList,
            out ICustomDestinationList? destinationList);

        if (resultCode != 0 || destinationList is null) return;

        destinationList.SetAppID(AppId);

        resultCode = destinationList.BeginList(out _, in JumpListInterop.IidObjectArray, out _);
        if (resultCode != 0) return;

        // Create Tasks collection
        if (JumpListInterop.CoCreateInstance(in JumpListInterop.ClsidEnumerableObjectCollection, IntPtr.Zero, 1, in JumpListInterop.ClsidEnumerableObjectCollection, out IObjectCollection? tasksCollection) == 0 && tasksCollection is not null)
        {
            // Add Volume Control Task
            if (CreateShellLink(exePath, "音量調整", "--volume-popup", out var volumeLink) == 0 && volumeLink is not null)
            {
                void* punk = ComInterfaceMarshaller<IShellLinkW>.ConvertToUnmanaged(volumeLink);
                tasksCollection.AddObject((nint)punk);
                Marshal.Release((nint)punk);
            }
            destinationList.AddUserTasks((IObjectArray)tasksCollection);
        }

        // Create Bookmarks Category
        if (JumpListInterop.CoCreateInstance(in JumpListInterop.ClsidEnumerableObjectCollection, IntPtr.Zero, 1, in JumpListInterop.ClsidEnumerableObjectCollection, out IObjectCollection? bookmarksCollection) == 0 && bookmarksCollection is not null)
        {
            // We use friendly name for matching, but maybe we should use ID? 
            // Arguments are limited in length, but ID should fit.
            foreach (var device in bookmarks.Take(10))
            {
                if (CreateShellLink(exePath, device.FriendlyName, $"--switch-device \"{device.FriendlyName}\"", out var deviceLink) == 0 && deviceLink is not null)
                {
                    void* punk = ComInterfaceMarshaller<IShellLinkW>.ConvertToUnmanaged(deviceLink);
                    bookmarksCollection.AddObject((nint)punk);
                    Marshal.Release((nint)punk);
                }
            }
            destinationList.AppendCategory("ブックマーク", (IObjectArray)bookmarksCollection);
        }

        destinationList.CommitList();
    }

    static int CreateShellLink(string exePath, string title, string arguments, out IShellLinkW? shellLink)
    {
        var resultCode = JumpListInterop.CoCreateInstance(
            in JumpListInterop.ClsidShellLink,
            IntPtr.Zero,
            1,
            in JumpListInterop.IidShellLink,
            out shellLink);

        if (resultCode == 0 && shellLink is not null)
        {
            shellLink.SetPath(exePath);
            shellLink.SetArguments(arguments);
            
            // Set Title via PropertyStore
            if (shellLink is IPropertyStore propertyStore)
            {
                var titleKey = new PropertyKey 
                { 
                    formatId = new("F29F885C-4EF1-4B3A-8393-31AE751B5448"), 
                    propertyId = 2 
                };

                var pv = new PropVariant();
                pv.variantType = 31; // VT_LPWSTR
                pv.pointerValue = Marshal.StringToCoTaskMemUni(title);

                propertyStore.SetValue(in titleKey, in pv);
                propertyStore.Commit();
                
                Marshal.FreeCoTaskMem(pv.pointerValue);
            }
        }
        return resultCode;
    }
}
