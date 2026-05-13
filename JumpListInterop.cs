using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SDS.Avalonia;

[GeneratedComInterface]
[Guid("6332DEBF-87B5-4670-90C0-5E57B408A49E")]
internal partial interface ICustomDestinationList
{
    [PreserveSig] int SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
    [PreserveSig] int BeginList(out uint pcMinSlots, in Guid riid, [MarshalUsing(typeof(ComInterfaceMarshaller<IObjectArray>))] out IObjectArray? ppv);
    [PreserveSig] int AppendCategory([MarshalAs(UnmanagedType.LPWStr)] string pszCategory, [MarshalUsing(typeof(ComInterfaceMarshaller<IObjectArray>))] IObjectArray poa);
    [PreserveSig] int AppendKnownCategory(int category);
    [PreserveSig] int AddUserTasks([MarshalUsing(typeof(ComInterfaceMarshaller<IObjectArray>))] IObjectArray poa);
    [PreserveSig] int CommitList();
    [PreserveSig] int GetRemovedDestinations(in Guid riid, out IntPtr ppv);
    [PreserveSig] int DeleteList([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
    [PreserveSig] int AbortList();
}

[GeneratedComInterface]
[Guid("92CA9DCD-5622-4bba-A805-5E9F541BD8C9")]
internal partial interface IObjectArray
{
    [PreserveSig] int GetCount(out uint pcObjects);
    [PreserveSig] int GetAt(uint uiIndex, in Guid riid, out IntPtr ppv);
}

[GeneratedComInterface]
[Guid("5632B1A4-E38A-400a-928A-D4CD63230295")]
internal partial interface IObjectCollection
{
    // IObjectArray
    [PreserveSig] int GetCount(out uint pcObjects);
    [PreserveSig] int GetAt(uint uiIndex, in Guid riid, out IntPtr ppv);

    // IObjectCollection
    [PreserveSig] int AddObject(nint punk);
    [PreserveSig] int AddFromArray([MarshalUsing(typeof(ComInterfaceMarshaller<IObjectArray>))] IObjectArray poaSource);
    [PreserveSig] int RemoveObjectAt(uint uiIndex);
    [PreserveSig] int Clear();
}

[GeneratedComInterface]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal partial interface IShellLinkW
{
    [PreserveSig] int GetPath(nint pszFile, int cch, out nint pfd, uint fFlags);
    [PreserveSig] int GetIDList(out IntPtr ppidl);
    [PreserveSig] int SetIDList(IntPtr pidl);
    [PreserveSig] int GetDescription(nint pszName, int cch);
    [PreserveSig] int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    [PreserveSig] int GetWorkingDirectory(nint pszDir, int cch);
    [PreserveSig] int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
    [PreserveSig] int GetArguments(nint pszArgs, int cch);
    [PreserveSig] int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
    [PreserveSig] int GetHotkey(out ushort pwHotkey);
    [PreserveSig] int SetHotkey(ushort wHotkey);
    [PreserveSig] int GetShowCmd(out int piShowCmd);
    [PreserveSig] int SetShowCmd(int iShowCmd);
    [PreserveSig] int GetIconLocation(nint pszIconPath, int cch, out int piIcon);
    [PreserveSig] int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
    [PreserveSig] int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
    [PreserveSig] int Resolve(IntPtr hwnd, uint fFlags);
    [PreserveSig] int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

internal static partial class JumpListInterop
{
    public static readonly Guid ClsidDestinationList = new("77F10CF0-3DB5-4966-B520-B7C54FD35ED6");
    public static readonly Guid ClsidEnumerableObjectCollection = new("2d3468c1-36a7-43b6-ac24-d3f02fd9607a");
    public static readonly Guid ClsidShellLink = new("00021401-0000-0000-C000-000000000046");

    public static readonly Guid IidCustomDestinationList = new("6332DEBF-87B5-4670-90C0-5E57B408A49E");
    public static readonly Guid IidObjectArray = new("92CA9DCD-5622-4bba-A805-5E9F541BD8C9");
    public static readonly Guid IidObjectCollection = new("5632B1A4-E38A-400a-928A-D4CD63230295");
    public static readonly Guid IidShellLink = new("000214F9-0000-0000-C000-000000000046");

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial void SetCurrentProcessExplicitAppUserModelID(string AppID);

    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(
        in Guid classId,
        IntPtr outerInstance,
        uint context,
        in Guid interfaceId,
        [MarshalUsing(typeof(ComInterfaceMarshaller<ICustomDestinationList>))] out ICustomDestinationList? result);

    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(
        in Guid classId,
        IntPtr outerInstance,
        uint context,
        in Guid interfaceId,
        [MarshalUsing(typeof(ComInterfaceMarshaller<IObjectCollection>))] out IObjectCollection? result);

    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(
        in Guid classId,
        IntPtr outerInstance,
        uint context,
        in Guid interfaceId,
        [MarshalUsing(typeof(ComInterfaceMarshaller<IShellLinkW>))] out IShellLinkW? result);
}
