using Microsoft.Windows.Storage;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Syllanote.Desktop;

internal static class ApplicationDataProvider
{
    private const int ErrorInsufficientBuffer = 122;
    private const int AppModelErrorNoPackage = 15700;

    public static ApplicationData Current { get; } = CreateApplicationData();

    public static string LocalPath
    {
        get
        {
            var localPath = Current.LocalPath;
            System.IO.Directory.CreateDirectory(localPath);
            return localPath;
        }
    }

    private static ApplicationData CreateApplicationData() =>
        HasPackageIdentity()
            ? ApplicationData.GetDefault()
            : ApplicationData.GetForUnpackaged("Andershgras", "Syllanote");

    private static bool HasPackageIdentity()
    {
        uint packageFullNameLength = 0;
        var result = GetCurrentPackageFullName(
            ref packageFullNameLength,
            null);

        return result switch
        {
            ErrorInsufficientBuffer => true,
            AppModelErrorNoPackage => false,
            _ => throw new Win32Exception(result)
        };
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(
        ref uint packageFullNameLength,
        StringBuilder? packageFullName);
}
