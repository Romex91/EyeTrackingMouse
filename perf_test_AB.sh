#!/bin/bash
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

echo A , B

N_A=0
N_B=0
for run in {1..25}
do
    A=`./bin/PerfBinaries/A/PerfTestRunner.exe`
    B=`./bin/PerfBinaries/B/PerfTestRunner.exe`

    if [ $A -gt $B ]
    then
        let N_B++
        echo -e  $A , $B ${GREEN} $(($A-$B)) ${NC}
    else
        let N_A++
        echo -e  $A , $B ${RED} $(($A-$B)) ${NC}
    fi

    B=`./bin/PerfBinaries/B/PerfTestRunner.exe`
    A=`./bin/PerfBinaries/A/PerfTestRunner.exe`

    if [ $A -gt $B ]
    then
        let N_B++
        echo -e  $A , $B ${GREEN} $(($A-$B)) ${NC}
    else
        let N_A++
        echo -e  $A , $B ${RED} $(($A-$B)) ${NC}
    fi
done

echo $N_A/$N_B

