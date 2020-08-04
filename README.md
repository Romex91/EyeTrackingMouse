# EyeTrackingMouse
Replaces computer mouse with combination of [Tobii Eye Tracker 4C/5](https://gaming.tobii.com/) and hotkeys. 

This is MVP. Please help me to find bugs.
The app autoupdates using Squirrel.Windows.

# Installation
Download and run Setup.exe here https://github.com/Romex91/EyeTrackingMouse/releases/latest

This will create an entry in [Apps & features] and schedule the app to run at Windows startup.

The application requires elevated privilegies for controlling mouse when system windows are open.

*The setup.exe is pretty outdated. 
Download the sources and compile them with **Visual Studio** to get a newer(and better) version of EyeTrackingMouse.
The new version is faster and more precise. It also has **ALWAYS_ON** mode toggled by double **Win** press.
Compile the new version yourself or wait until I make a release (might take a while).*

# Usage
5-minute User Guide:

[![User Guide](https://github.com/Romex91/EyeTrackingMouse/blob/master/user_guide_preview.png)](https://youtu.be/aKi3Qr7T764)

To start controlling cursor press Win. 

The keys below work only when Win is pressed:
```
W/A/S/D - adjust cursor position.
J - Left button
K - Right button
H - Scroll up
N - Scroll down
< - Scroll left
> - scroll right
```

To open Windows Start Menu press and release Win quickly. 

![Default Key Bindings](https://github.com/Romex91/EyeTrackingMouse/blob/master/default_key_bindings.png)

You can reassign key bindings in the settings window (click Tray icon)

# Hight Cpu Load Issue (rare case)
EyeTrackingMouse freezes when you 100% load all the cores of your CPU. Most users will never face this issue. It appears only when you are doing something special (e.g. Chromium compilation). This problem happens because Tobii service has **Normal** process priority by default.

**Changing process priorities may have unexpected consequences. Do it on your own risk.**

To address the issue set **High** process priority for:
1. All processes starting from Tobii
2. EyeTrackingMouse.exe

You can do it in *Windows Task Manager->Details*, but this is a temporary solution.
For permanent solution install *System Explorer* and set priorities with enabled *Permanent* checkbox.

# Support the project
https://www.patreon.com/EyeTrackingMouse
