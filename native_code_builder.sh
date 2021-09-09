#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

TARGET="$DIR/Artomatix.NativeCodeBuilder"
PERMS=`stat -c "%a" $TARGET`

# NuGet packages don't respect file permissions
if [[ ! $PERMS == 7* ]]; then
    # Make executable
    chmod u+x $TARGET
fi


$TARGET $@
