#!/bin/bash


PROJDIR=$1
CONFIG=$2


DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ -z "$DISPLAY" ]; then
    PRE_BUILD_NO_TERM=true
fi

termCommand="xterm -e"
type mate-terminal >/dev/null 2>&1 && termCommand="mate-terminal --title=Building_native_code... --disable-factory -x bash -c"
# no gnome-terminal support because they broke --disable-factory https://github.com/IgnorantGuru/spacefm/issues/428

if [ -z "$PRE_BUILD_NO_TERM" ]; then
    STATUSFILE=$(mktemp)
    $termCommand "PRE_BUILD_NO_TERM=true /bin/bash \"$0\" \"$1\" \"$2\"; EXIT_STATUS=\$?; echo; echo EXIT STATUS: \$EXIT_STATUS; echo \$EXIT_STATUS > $STATUSFILE; read -n1 -r -p 'Press any key to continue...'"
    if [ $? -ne 0 ]; then exit 1; fi
    STATUS=$(cat $STATUSFILE)
    rm $STATUSFILE
    exit $STATUS
fi


if [ ! -e "`tail -n 2 "$PROJDIR/native_code_setting.txt" | head -n 1`" ]; then
    echo "#################################################"
    echo "Your native source code directory doesn't exist!"
    echo "Edit this file to change it: $PROJDIR/native_code_setting.txt"
    echo "Current contents:"
    cat "$PROJDIR/native_code_setting.txt"

    exit 1
fi

NATIVE_DIR="$( cd `tail -n 2 "$PROJDIR/native_code_setting.txt" | head -n 1` && pwd )"
BUILD_DIR="$NATIVE_DIR/build_$CONFIG"
C_DLL="$BUILD_DIR/lib`tail -n 1 "$PROJDIR/native_code_setting.txt"`.so"

if [ ! -e "$BUILD_DIR" ]; then
    mkdir "$BUILD_DIR"
    if [ $? -ne 0 ]; then exit 1; fi
fi

cd "$BUILD_DIR"
if [ $? -ne 0 ]; then exit 1; fi

cmake .. -DCMAKE_BUILD_TYPE=$CONFIG
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
