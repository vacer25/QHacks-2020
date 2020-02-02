#!/bin/sh
for Dir in `find ./* -type d` ; do
    i=0
    Prefix=""
    case "$Dir" in
        *verif*) Prefix="v" ;;
    esac
    for File in $Dir/*.jpg ; do
        mv "$File" "$Dir/$Prefix$i.jpg"
        i=$(($i+1))
    done
done