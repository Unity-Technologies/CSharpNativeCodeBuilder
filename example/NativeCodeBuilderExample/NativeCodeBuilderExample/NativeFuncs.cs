using Stugo.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeCodeBuilderExample
{
    class NativeFuncs
    {
        private static NativeFuncs initNative()
        {
            var dllPath = Path.GetFullPath("ExampleNativeCode.so");
            var zipStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("NativeCodeBuilderExample.embedded_files.binaries.zip");
            NativeBinaryManager.NativeBinaryManager.ExtractNativeBinary(zipStream, dllPath);

            UnmanagedModuleCollection.Instance.LoadModule<NativeFuncs>(dllPath);

            return UnmanagedModuleCollection.Instance.GetModule<NativeFuncs>();
        }

        public static readonly NativeFuncs inst = initNative();

        public delegate int add_two_nums_t(int a, int b);
        [EntryPoint("add_two_nums")]
        public add_two_nums_t add_two_nums;
    }
}
