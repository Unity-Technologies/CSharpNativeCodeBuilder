using System;
using System.IO.Compression;
using System.IO;

namespace NativeBinaryManager
{
    public static class NativeBinaryManager
    {
        public static void ExtractNativeBinary(Stream resourceStream, string destPath)
        {
            var zipStream = resourceStream;

            // using ZipStorer (nuget pkg) here instead of ZipArchive (.NET built in) because older versions of mono don't support it
            using (var zip = ZipStorer.Open(zipStream, FileAccess.Read))
            {
                string nativeCodeFilename = "native_code_" + getCurrentPlatform () + "_" + getArchString();
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

        private static string getCurrentPlatform()
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
                        return "osx";
                    else
                        return "linux";

                    case PlatformID.MacOSX:
                    return "osx";

                    default:
                    return "windows";
            }
        }

        private static string getArchString()
        {
            if (Environment.Is64BitProcess)
                return "x64";
            else
                return "x86";
        }
    }
}