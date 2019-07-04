using System;
using System.IO;
using System.IO.Compression;

namespace Artomatix.NativeBinaryManager
{
    public enum Platform
    {
        Windows,
        OSX,
        Linux
    }

    public static class Ext
    {
        public static string ToFriendlyString(this Platform pt)
        {
            switch (pt)
            {
                case Platform.Linux:
                    return "Linux";

                case Platform.OSX:
                    return "OSX";

                default:
                case Platform.Windows:
                    return "Windows";
            }
        }
    }

    public static class NativeBinaryExtractor
    {
        public static void ExtractNativeBinary(Stream resourceStream, string destPath)
        {
            var zipStream = resourceStream;

            // using ZipStorer (nuget pkg) here instead of ZipArchive (.NET built in) because older versions of mono don't support it
            using (var zip = ZipStorer.Open(zipStream, FileAccess.Read))
            {
                string nativeCodeFilename = $"native_code_{GetCurrentPlatform().ToFriendlyString().ToLower()}_{GetArchString()}";
                foreach (var entry in zip.ReadCentralDir())
                {
                    if (entry.FilenameInZip == nativeCodeFilename)
                    {
                        using (var f = File.OpenWrite(destPath))
                            zip.ExtractFile(entry, f);

                        return;
                    }
                }

                throw new PlatformNotSupportedException("This package does not contain native code for your platform");
            }
        }

        public static Platform GetCurrentPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                    {
                        return Platform.OSX;
                    }
                    else
                    {
                        return Platform.Linux;
                    }

                case PlatformID.MacOSX:
                    return Platform.OSX;

                default:
                    return Platform.Windows;
            }
        }

        public static string GetArchString()
        {
            if (Environment.Is64BitProcess)
                return "x64";
            else
                return "x86";
        }
    }
}