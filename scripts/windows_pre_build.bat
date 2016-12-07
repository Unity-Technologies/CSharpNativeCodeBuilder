set "PROJDIR=%~1"
set "CONFIG=%~2"
set "REALARCH=%~3"

if %REALARCH%==x64 (
	set "ARCH= Win64"
) else (
	if %REALARCH%==x86 (
		set "ARCH="
	) else (
		echo "bad ARCH value"
		exit 1
	)
)

set CURRDIR=%CD%


echo "--------------------------" %PROJDIR% %CONFIG% %REALARCH%

cd "%PROJDIR%"

set "NATIVE_DIR="
for /F "skip=1 delims=" %%i in (native_code_setting.txt) do if not defined NATIVE_DIR set "NATIVE_DIR=%%i"

:: replace forward with backslashes
set "NATIVE_DIR=%NATIVE_DIR:/=\%"
:: make absolute
set "NATIVE_DIR=%PROJDIR%%NATIVE_DIR%"

set "LIBNAME="
for /F "skip=2 delims=" %%i in (native_code_setting.txt) do if not defined LIBNAME set "LIBNAME=%%i"

set "CMAKE_ARGS="
for /F "skip=3 delims=" %%i in (native_code_setting.txt) do if not defined CMAKE_ARGS set "CMAKE_ARGS=%%i"


cd %CURRDIR%

if not exist "%NATIVE_DIR%" ( 
    echo "#################################################"
    echo "Your native source code directory doesn't exist!"
    echo "Edit this file to change it: %PROJDIR%\native_code_setting.txt"
    echo "Current evaluated setting: %NATIVE_DIR%"
    echo "Current contents:"
    type "%PROJDIR%\native_code_setting.txt"

    exit 1
)


set "BUILD_DIR=%NATIVE_DIR%build_%CONFIG%_%REALARCH%"
set "C_DLL=%BUILD_DIR%\inst\lib\%LIBNAME%.dll"

echo %BUILD_DIR%

if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"


cd "%BUILD_DIR%"

set "binfile=native_code_windows_%REALARCH%"
:: delete the old one first, just to be sure
if exist "%PROJDIR%\embedded_files\%binfile%" del "%PROJDIR%\embedded_files\%binfile%"

echo cmake .. %CMAKE_ARGS% -DCMAKE_BUILD_TYPE=%CONFIG% -DCMAKE_INSTALL_PREFIX=inst -G "Visual Studio 14%ARCH%"
cmake .. %CMAKE_ARGS% -DCMAKE_BUILD_TYPE=%CONFIG% -DCMAKE_INSTALL_PREFIX=inst -G "Visual Studio 14%ARCH%"
if %errorlevel% neq 0 exit 1
cmake --build . --target install --config %CONFIG%
if %errorlevel% neq 0 (
    echo "If you see an error above about something not existing and mentioning the install target then you will need to add installing to your cmake script."
    echo "the simplest way to do this is:"
    echo "install (TARGETS YOURTARGET ARCHIVE DESTINATION lib LIBRARY DESTINATION lib RUNTIME DESTINATION lib)"
    
    exit 1
)

echo copy "%C_DLL%" "%PROJDIR%\embedded_files\%binfile%"
copy "%C_DLL%" "%PROJDIR%\embedded_files\%binfile%"
if %errorlevel% neq 0 exit 1

if exist "%PROJDIR%\embedded_files\binaries.zip" del "%PROJDIR%\embedded_files\binaries.zip"

echo powershell -ExecutionPolicy ByPass "%~dp0\windows_zip.ps1" "%PROJDIR%\embedded_files"
powershell -ExecutionPolicy ByPass "%~dp0\windows_zip.ps1" "%PROJDIR%\embedded_files"
if %errorlevel% neq 0 exit 1

cd %CURRDIR%

echo "------------NATIVE CODE BUILD COMPLETED SUCCESSFULLY"
exit 0