using CommandLine;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Artomatix.NativeCodeBuilder
{
    public class Program
    {
        public class CustomXMLWriter : XmlTextWriter
        {
            public CustomXMLWriter(Stream stream) : base(stream, Encoding.UTF8)
            {
                this.Formatting = Formatting.Indented;
            }

            public override void WriteEndElement()
            {
                base.WriteFullEndElement();
            }
        }

        public enum Error
        {
            Success = 0,
            InvalidArgs = 1,
            NativeSettingsNotFound = 2,
            NativeCodePathNotFound = 3,
            CMakeConfigureStepError = 4,
            CMakeBuildError = 5
        }

        [Verb(nameof(BuildArgs), true)]
        private class BuildArgs
        {
            [Value(0, Required = true, MetaName = nameof(ProjectDir))]
            public string ProjectDir { get; set; }

            [Value(1, Required = true, MetaName = nameof(Target))]
            public string Target { get; set; }

            [Value(2, MetaName = nameof(Generator))]
            public string Generator { get; set; }
        }

        [Verb("create", false)]
        public class CreateArgs
        {
            [Option('p', "path")]
            public string PathToNativeCode { get; set; }

            [Option('t', "targets")]
            public IEnumerable<string> Targets { get; set; }

            [Option('c', "cmakeArgs")]
            public string CMakeArgs { get; set; }

            [Option('o', "outputPath", Required = true)]
            public string OutputPath { get; set; }

            [Option('b', "buildFolderBase")]
            public string BuildFolderBase { get; set; }

            [Option('y', "yes")]
            public bool Yes { get; set; }
        }

        public static int Main(string[] args)
        {
            var buildOpts = default(BuildArgs);
            var createOpts = default(CreateArgs);

            Parser.Default.ParseArguments(args,
                typeof(BuildArgs),
                typeof(CreateArgs))
                .WithParsed<BuildArgs>(parsed =>
                {
                    buildOpts = parsed;
                }).WithParsed<CreateArgs>(parsed =>
                {
                    createOpts = parsed;
                });
            if (buildOpts != null)
            {
                return HandleBuildCommand(buildOpts);
            }
            else if (createOpts != null)
            {
                return HandleCreateCommand(createOpts);
            }

            return 1;
        }

        private static int HandleCreateCommand(CreateArgs args)
        {
            var settings = new NativeCodeSettings()
            {
                CMakeArguments = args.CMakeArgs ?? "",
                DLLTargets = args.Targets.ToArray(),
                PathToNativeCodeBase = args.PathToNativeCode ?? "",
                BuildPathBase = args.BuildFolderBase ?? ""
            };

            bool consentGiven = !File.Exists(args.OutputPath) || args.Yes;

            if (!consentGiven)
            {
                Console.WriteLine($"File exists at {args.OutputPath}");
                Console.Write("Overwrite? [yN]: ");
            }

            while (!consentGiven)
            {
                var key = (char)Console.Read();

                if (key == 'n' || key == 'N' || key == '\n' || key == '\r')
                {
                    Console.WriteLine("Cancelling...");
                    return 0;
                }
                else if (key == 'y' || key == 'Y')
                {
                    consentGiven = true;
                }
                else
                {
                    Console.Write("Please enter yes or no [yN]:");
                }
            }
            var serializer = new XmlSerializer(typeof(NativeCodeSettings));

            using (var fileStream = File.Open(args.OutputPath, FileMode.Create))
            {
                var writer = new CustomXMLWriter(fileStream);

                serializer.Serialize(writer, settings);
            }

            return 0;
        }

        private static int HandleBuildCommand(BuildArgs args)
        {
            var projectDir = args.ProjectDir;
            var target = args.Target;

            string generator = null;
            string buildTools = "v141";
            bool vs2019 = false;

            if (!String.IsNullOrEmpty(args.Generator))
            {
                generator = args.Generator;

                if (generator == "Visual Studio Cu" || generator == "Visual Studio Current")
                {
                    generator = "Visual Studio 16";
                    buildTools = "v142";
                    vs2019 = true;
                }
            }

            var platform = NativeBinaryExtractor.GetCurrentPlatform();

            var originalArch = NativeBinaryExtractor.GetArchString();

            var arch = Environment.Is64BitProcess && platform == Platform.Windows && !vs2019
                ? "Win64"
                : string.Empty;

            if (vs2019)
            {
                arch = originalArch;
            }

            var nativeSettingsPath = Path.Combine(projectDir, "NativeCodeSettings.xml");

            if (!File.Exists(nativeSettingsPath))
            {
                Console.Error.WriteLine($"Native settings file not found: {nativeSettingsPath}");

                return (int)Error.NativeSettingsNotFound;
            }

            var serializer = new XmlSerializer(typeof(NativeCodeSettings));

            INativeCodeSettings nativeSettings = null;
            using (var file = File.OpenRead(nativeSettingsPath))
            {
                nativeSettings = (NativeCodeSettings)serializer.Deserialize(file);
            }

            var nativeCodePath = Path.Combine(projectDir, nativeSettings.PathToNativeCodeBase);

            if (!Directory.Exists(nativeCodePath))
            {
                Console.Error.Write(
                    $"Your native source code directory ({nativeCodePath}) doesn't exist!\n" +
                    $"Edit this file to change it: {nativeSettingsPath}\n" +
                    $"Current contents:{string.Join(Environment.NewLine, nativeSettings)}");

                return (int)Error.NativeCodePathNotFound;
            }

            var buildDir = Path.Combine(nativeCodePath, $"{nativeSettings.BuildPathBase}_{target}_{originalArch}");
            var cmakeArgs = nativeSettings.CMakeArguments;

            Console.WriteLine("buildDir is " + buildDir);
            Console.WriteLine("CMakeArgs are " + cmakeArgs);

            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            Directory.SetCurrentDirectory(buildDir);

            var generatorArgument = generator != null ? $"-G \"{generator} {arch}\" " : "";

            string cfargs;

            if (vs2019)
            {
                cfargs = ".. " +
                    $"-G \"{generator}\" " +
                    $"-A {arch} " +
                    $"-T {buildTools} " +
                    $"{cmakeArgs} " +
                    $"-DCMAKE_INSTALL_PREFIX=inst";
            }
            else
            {
                cfargs = ".. " +
                $"{cmakeArgs} " +
                generatorArgument +
                "-DCMAKE_INSTALL_PREFIX=inst";
            }

            Console.WriteLine($"CMake args are {cfargs}");
            var cmakeConfigureLaunchArgs = new ProcessStartInfo("cmake", cfargs)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var cmakeConfigureProcess = new Process();

            cmakeConfigureProcess.StartInfo = cmakeConfigureLaunchArgs;

            cmakeConfigureProcess.OutputDataReceived += WriteOutput;
            cmakeConfigureProcess.ErrorDataReceived += WriteOutput;

            cmakeConfigureProcess.Start();

            cmakeConfigureProcess.BeginOutputReadLine();
            cmakeConfigureProcess.BeginErrorReadLine();

            cmakeConfigureProcess.WaitForExit();

            if (cmakeConfigureProcess.ExitCode != 0)
            {
                Console.Error.WriteLine($"CMake exited with non-zero error code: {cmakeConfigureProcess.ExitCode}.");
                Console.Error.WriteLine($"Deleting the {buildDir} directory might fix this.");

                return (int)Error.CMakeConfigureStepError;
            }

            var cmakeBuildLaunchArgs = new ProcessStartInfo("cmake", $"--build . --target install --config {target}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var cmakeBuildProcess = new Process();
            cmakeBuildProcess.StartInfo = cmakeBuildLaunchArgs;

            cmakeBuildProcess.OutputDataReceived += WriteOutput;
            cmakeBuildProcess.ErrorDataReceived += WriteOutput;

            cmakeBuildProcess.Start();

            cmakeBuildProcess.BeginOutputReadLine();
            cmakeBuildProcess.BeginErrorReadLine();

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

            var embeddedFilesPath = Path.Combine(projectDir, "embedded_files");

            if (!Directory.Exists(embeddedFilesPath))
            {
                Directory.CreateDirectory(embeddedFilesPath);
            }

            for (var index = 0; index < nativeSettings.DLLTargets.Length; index++)
            {
                var prefix = platform != Platform.Windows ? "lib" : string.Empty;
                var dllToCopy = Path.Combine(buildDir, "inst", "lib", $"{prefix}{nativeSettings.DLLTargets[index]}.{dllExtension}");
                var filename = Path.GetFileNameWithoutExtension(dllToCopy);
                File.Copy(dllToCopy, Path.Combine(embeddedFilesPath, $"{filename}.dll"), overwrite: true);
            }

            Console.WriteLine("Work complete");

            return (int)Error.Success;
        }

        private static void WriteOutput(object sender, DataReceivedEventArgs evt)
        {
            Console.WriteLine($"CMake: {evt.Data}");
        }
    }
}