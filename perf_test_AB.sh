#!/bin/bash
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

echo new utility `./bin/PerfBinaries/B/PerfTestRunner.exe utility`
echo A , B
for run in {1..10}
do
    Sleep 1
    A=`./bin/PerfBinaries/A/PerfTestRunner.exe`
    Sleep 1
    B=`./bin/PerfBinaries/B/PerfTestRunner.exe`

    if [ $A -gt $B ]
    then
        echo -e  $A , $B ${GREEN} $(($A-$B)) ${NC}
    else
        echo -e  $A , $B ${RED} $(($A-$B)) ${NC}
    fi
done

