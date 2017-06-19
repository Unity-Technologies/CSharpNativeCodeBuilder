# NativeCodeBuilder
[![NuGet](https://img.shields.io/nuget/v/NativeCodeBuilder.svg)](https://www.nuget.org/packages/NativeCodeBuilder)

This project is designed to ease the pain of writing interop code in c#.
The use case is this: you have some c/c++ code that you want to use from c#, but it's custom, so you can't just expect it to be installed in the user's library path
somewhere and blindly p/invoke it. This brings a huge mess of concerns, because now you need drag around a dll/so file everywhere you run your code, and if it's a
library, then it's a real mess because users of that library need to drag it around too. Not to mention running cross platform. 

This project should take care of most of this mess, by packaging everything up into one c# managed dll, that can be used on any platform without recompiling or custom
install steps. You can even whack it in a nuget package.

## How does it work?
This project essentially does 3 things, build the native code, pack it for use at runtime, and load the code at runtime.
### Building the native code
This package is available as a [nuget package](https://www.nuget.org/packages/NativeCodeBuilder). When installed, it adds a pre-build script to your csproj file, that
will build the native code with [cmake](https://cmake.org), producing a single dll/so dependent on platform. Options for the building of native code are specified in 
the native\_code\_setting.txt file that is placed in your project on install of this package.
### Pack native code for use at runtime
This dll/so is then copied into a special folder (embedded\_files) with a filename that denotes the platform/architecure of that native code file. For example a windows
32-bit build would be placed in embedded\_files/native\_code\_windows\_x86, while an x64 linux build would be saved in embedded\_files/native\_code\_linux\_x64.
All files in that folder (from whatever architectures/platforms are there at build time), will be zipped into a single file, which is then added to the main c# project as
an [EmbeddedResource](https://support.microsoft.com/en-us/help/319292/how-to-embed-and-access-resources-by-using-visual-c).
To support multiple platforms/architectures, just build on those architectures and copy the native code file for each of them into this folder on one of the build
mahcines, and then do the build with all of them present.
### Load the code at runtime
At runtime, the NativeBinaryManager class (installed in a separate [nuget package](https://www.nuget.org/packages/NativeBinaryManager) automatically, as a dependency of
the NativeCodeBuilder nuget package) is used to select the correct binary, and unzip it to a temporary location (currently just the working directory, but that's a bit
nasty), then it can be loaded from there, using whatever method you want (I recommend using stugo.interop  [[1]](https://github.com/gordonmleigh/Stugo.Interop)
[[2]](https://www.nuget.org/packages/Stugo.Interop)), but there is one awkwardness to consider: using p/invoke directly will probably not work, as the c# runtime expects
the native dll/so to be present immediately on program start (actually, at first access of the class that contains the p/invoke code), so it will likely fail by
attempting to load the dll before it's been extracted. Using stugo.interop, or employing similar methods to those used there can fix this problem.

## Example project
See the [example/](example/) directory for a simple example project.
For a real life example, see [ArtomatixImageLoader](https://github.com/Artomatix/ArtomatixImageLoader/tree/master/bindings/csharp/ArtomatixImageLoader).

## Limitations
- Only works with cmake build system.
- Currently only work with building c# binaries with a specified architecture, but this limitation is arbitrary and can/should be removed to allow AnyCPU builds.
- Currently, we only support packing a single dll/so. This means no dynamically linked dependencies, you just have to static link everything into one binary.
This can be made easier by using the awesome [hunter](https://github.com/ruslo/hunter) project, which is a package manager for c++ that downloads and builds third party
dependencies. 
- On windows you need to provide an install target in your cmake files, the build scripts will warn you/show you what to do if you don't.
- Doesn't magically make your native code work cross platform :p

## Usage
- Create c# project + switch it from AnyCPU to x64.
- Install NativeCodeBuilder nuget package
- Create a folder native\_code in the root of your _solution_ directory (not project) + fill it with your native code (+ CMakeLists.txt). Alternatively, you can place
your native code somewhere else, and edit the native\_code\_setting.txt file accordingly.
- Add the following to the end of your cmake script (if you haven't already got an install target specified):
```cmake
install (TARGETS YOUR_TARGETS_NAME_HERE
         ARCHIVE DESTINATION lib
         LIBRARY DESTINATION lib
         RUNTIME DESTINATION lib)
```
- This will add an install target to your native code (make install, or a project called INSTALL in your vs solution on windows), which when run will place the built
result in the specified install folder.
- Edit native\_code\_setting.txt and change the second line to the name you specified for your project in cmake.
- Create the folder embedded\_files in your _project_ directory. 
- Try to build the c# project. If successful, you should see two files in the embedded\_files directory, binaries.zip, and a specific binary for the platform you
just built for.
- Add binaries.zip to the project. The easiest way to do this (in visual studio) is to just drag the embedded\_files folder from explorer over the project in solution
explorer in vs. Then, drag binaries.zip from explorer into the embedded\_files folder in solution explorer. Left click on binaries.zip in solution explorer to open its
properties window, then change Build Action to EmbeddedResource. The steps should be similar for Monodevelop, or you can just have a look at the
[example project](example/) and copy out the bits of csproj generated.
- Install the [Stugo.Interop](https://www.nuget.org/packages/Stugo.Interop) nuget package.
- The easiest way to ensure we load the functions before we use them is to use  singleton, and extract the binaries/load the functions in the constructor, like so:
```c#
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
    
    // ... add functions here later
    
}
```
- Note: UnmanagedModuleCollection is a class from Stugo.Interop, also you should replace the path (ExampleNativeCode.so) with an appropriate name (it doesn't matter if
you use the wrong extension on one platform, unconditionally saving as .so or .dll is fine). You might also want to place the dll in some temporary directory instead of
the current dir. Also, you should replace the NativeCodeBuilderExample in NativeCodeBuilderExample.embedded_files.binaries.zip with the name of the c# assembly that
contains the EmbeddedResource zip file.
- The next step is to add the functions. We do this by declaring a delegate that specifies the signature of the function, then instantiating the delegate with a special
attribute that instructs UnmanagedModuleCollection to load the instance from the dll:
```c#
public delegate int add_two_nums_t(int a, int b);
[EntryPoint("add_two_nums")]
public add_two_nums_t add_two_nums;
```
- Now we can access the function just like it was a c# function, eg: `int a = NativeFuncs.inst.add_two_nums(1, 2);`
