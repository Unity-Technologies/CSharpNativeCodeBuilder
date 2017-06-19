#!/bin/bash


PROJDIR=$1
CONFIG=$2


DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ ! -e "`sed -n 2p "$PROJDIR/native_code_setting.txt"`" ]; then
    echo "#################################################"
    echo "Your native source code directory doesn't exist!"
    echo "Edit this file to change it: $PROJDIR/native_code_setting.txt"
    echo "Current contents:"
    cat "$PROJDIR/native_code_setting.txt"

    exit 1
fi

NATIVE_DIR="$( cd `sed -n 2p "$PROJDIR/native_code_setting.txt"` && pwd )"
BUILD_DIR="$NATIVE_DIR/build_$CONFIG"
C_DLL="$BUILD_DIR/lib`sed -n 3p "$PROJDIR/native_code_setting.txt"`.so"
CMAKE_ARGS=`sed -n 4p "$PROJDIR/native_code_setting.txt"`

if [ ! -e "$BUILD_DIR" ]; then
    mkdir "$BUILD_DIR"
    if [ $? -ne 0 ]; then exit 1; fi
fi

cd "$BUILD_DIR"
if [ $? -ne 0 ]; then exit 1; fi

echo cmake .. $CMAKE_ARGS -DCMAKE_BUILD_TYPE=$CONFIG
cmake .. $CMAKE_ARGS -DCMAKE_BUILD_TYPE=$CONFIG
if [ $? -ne 0 ]; then exit 1; fi

make
if [ $? -ne 0 ]; then exit 1; fi

if [ ! -e "$PROJDIR/embedded_files" ]; then
    mkdir "$PROJDIR/embedded_files"
    if [ $? -ne 0 ]; then exit 1; fi
fi

cp "$C_DLL" "$PROJDIR/embedded_files/native_code_linux_x64"
if [ $? -ne 0 ]; then exit 1; fi

cd "$PROJDIR/embedded_files"
if [ $? -ne 0 ]; then exit 1; fi

rm binaries.zip >/dev/null 2>&1
zip -r binaries.zip . -x ".gitignore" -x binaries.zip
if [ $? -ne 0 ]; then exit 1; fi

exit 0
