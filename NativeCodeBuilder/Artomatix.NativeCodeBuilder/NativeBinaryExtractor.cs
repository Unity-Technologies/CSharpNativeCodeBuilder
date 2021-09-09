using System;
using System.IO;

namespace Artomatix.NativeCodeBuilder
{
    public enum Platform
    {
        Windows,
        OSX,
        Linux
    }

    internal class NativeBinaryExtractor
    {
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