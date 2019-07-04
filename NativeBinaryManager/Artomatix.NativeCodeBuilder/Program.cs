using Artomatix.NativeBinaryManager;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Artomatix.NativeCodeBuilder
{
    public class Program
    {
        public enum Error
        {
            Success = 0,
            InvalidArgs = 1,
            NativeSettingsNotFound = 2,
            NativeCodePathNotFound = 3,
            CMakeConfigureStepError = 4,
            CMakeBuildError = 5
        }

        public static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine($"Invalid Arguments\n" +
                    $"    Usage: {Path.GetFileName(Assembly.GetExecutingAssembly().Location)} pathToProject Config");

                return (int)Error.InvalidArgs;
            }

            var projectDir = args[0];
            var target = args[1];

            string generator = null;

            if (args.Length > 2)
            {
                generator = args[2];
            }

            var platform = NativeBinaryExtractor.GetCurrentPlatform();

            var originalArch = NativeBinaryExtractor.GetArchString();

            var arch = Environment.Is64BitProcess && platform == Platform.Windows
                ? "Win64"
                : string.Empty;

            var nativeSettingsPath = $"{projectDir}/native_code_setting.txt";

            if (!File.Exists(nativeSettingsPath))
            {
                Console.Error.WriteLine($"Native settings file not found: {nativeSettingsPath}");

                return (int)Error.NativeSettingsNotFound;
            }

            var nativeSettings = File.ReadAllLines(nativeSettingsPath);

            var nativeCodePath = Path.Combine(projectDir, nativeSettings[1]);

            if (!Directory.Exists(nativeCodePath))
            {
                Console.Error.Write(
                    "Your native source code directory doesn't exist!\n" +
                    $"Edit this file to change it: {nativeSettingsPath}\n" +
                    $"Current contents:{string.Join("", nativeSettings)}");

                return (int)Error.NativeCodePathNotFound;
            }

            var buildDir = Path.Combine(nativeCodePath, $"build_{target}_{originalArch}");
            var cmakeArgs = nativeSettings.Last();

            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            Directory.SetCurrentDirectory(buildDir);

            var generatorArgument = generator != null ? $"-G \"{generator} {arch}\" " : "";

            var cfargs = ".. " +
                $"{cmakeArgs} " +
                $"-DCMAKE_BUILD_TYPE=${target} " +
                generatorArgument +
                "-DCMAKE_INSTALL_PREFIX=inst";

            var cmakeConfigureLaunchArgs = new ProcessStartInfo("cmake",
                cfargs)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var cmakeConfigureProcess = Process.Start(cmakeConfigureLaunchArgs);

            cmakeConfigureProcess.WaitForExit();

            if (cmakeConfigureProcess.ExitCode != 0)
            {
                Console.Error.WriteLine($"CMake exited with non-zero error code: {cmakeConfigureProcess.ExitCode}");

                return (int)Error.CMakeConfigureStepError;
            }

            var cmakeBuildLaunchArgs = new ProcessStartInfo("cmake", $"--build . --target install --config {target}")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var cmakeBuildProcess = Process.Start(cmakeBuildLaunchArgs);
            cmakeBuildProcess.WaitForExit();

            if (cmakeBuildProcess.ExitCode != 0)
            {
                Console.Error.WriteLine(
                    "If you see an error above about something not existing " +
                    "and mentioning the install target then you will need to add installing to your cmake script." +
                    "The simplest way to do this is:" +
                    "install (TARGETS YOURTARGET ARCHIVE DESTINATION lib LIBRARY DESTINATION lib RUNTIME DESTINATION lib)");

                return (int)Error.CMakeBuildError;
            }

            var dllExtension = platform == Platform.Windows ? "dll" : "so";

            var embeddedFilesPath = $"{projectDir}/embedded_files";

            if (!Directory.Exists(embeddedFilesPath))
            {
                Directory.CreateDirectory(embeddedFilesPath);
            }

            for (var index = 2; index < nativeSettings.Length - 1; index++)
            {
                var dllToCopy = $"{buildDir}/inst/lib/{nativeSettings[index]}.{dllExtension}";
                File.Copy(dllToCopy, Path.Combine(embeddedFilesPath, Path.GetFileName(dllToCopy)), overwrite: true);
            }

            Console.WriteLine("Work complete");

            return (int)Error.Success;
        }
    }
}