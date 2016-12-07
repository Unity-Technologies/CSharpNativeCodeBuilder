all: clean
	cd NativeBinaryManager && xbuild NativeBinaryManager.sln /target:Build /p:Configuration=Release
	nuget pack NativeBinaryManager.nuspec
	nuget pack NativeCodeBuilder.nuspec

clean:
	-rm NativeBinaryManager*.nupkg
	-rm NativeCodeBuilder*.nupkg
	cd NativeBinaryManager && xbuild NativeBinaryManager.sln /target:clean /p:Configuration=Release
