#!/bin/bash
set -e

git stash
'/C/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Current/Bin/MSBuild.exe' eye_tracking_mouse.sln -p:Configuration=Release
./prepare_perf_binary.sh A

git stash apply

'/C/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Current/Bin/MSBuild.exe' eye_tracking_mouse.sln -p:Configuration=Release
./prepare_perf_binary.sh B

./perf_test_AB.sh