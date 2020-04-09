#!/bin/bash
echo A , B
for run in {1..10}
do
    Sleep 1
    ./bin/PerfBinaries/A/PerfTestRunner.exe
    A=$?
    Sleep 1
    ./bin/PerfBinaries/B/PerfTestRunner.exe
    B=$?
    echo $A , $B
done

