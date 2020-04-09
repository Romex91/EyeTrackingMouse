#!/bin/bash
if [ "$1" == "A" ] || [ "$1" == "B" ]
then
    directory_path=./bin/PerfBinaries/$1

    if [ -d $directory_path ]
    then
        rm -r  $directory_path
    fi
    mkdir $directory_path
    cp -r ./bin/x64/Release/* $directory_path
else
    echo cannot run without A or B argument
fi