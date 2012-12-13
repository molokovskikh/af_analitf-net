#!/bin/sh
unset temp
unset tmp
find -iname "*.Test.dll" -ipath "*bin*" | xargs nunit-console-x86 /nologo $*
