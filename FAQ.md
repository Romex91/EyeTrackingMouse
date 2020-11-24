### Is it a must-have?

It is definitely a must-have if you type a lot.

### Why Windows shows Unknown Publisher warning?

Windows show Unknown Publisher for every exe without a signature. I cannot afford an App Signing Certificate at this moment. Let's buy it together: https://www.patreon.com/EyeTrackingMouse

### Does it fully replace mouse?

Yes!
...and no.

EyeTrackerMouse is absolutely better than mouse when you are touch-typing. It is better than touchpad in every task I can think of. I stopped using regular mouse and I don't have it connected to my PC and I'm happy with it.
But, some tasks are still better to do with mouse (videogames and **"pixel-hunting"** tasks).

It is 100% possible to use EyeTrackerMouse for every task (except playing 3d shooters). But, it is still useful to keep a regular mouse connected.

### Does it support Linux?

No. It is windows only. It works with Virtual-Box though and it is possible to make it working on VMWare (PR needed). If you want this app on Linux, hire me and I'll port it;)

### Does it support multiple displays?

No. But you can use EyeTrackingMouse on the main display and have a regular mouse to work with other displays.
It is a very useful setup. You can keep all clickable stuff on the main display and use hotkeys for everything else.

### Why does it require elevated privilegies?

It requires elevated privilegies to control the cursor when system windows are open. (Windows allows only privileged apps to interact with Task Manager).

### Why do custom key bindings require a driver installation?

WinAPI doesn't allow any process to truly intercept keypresses. Different applications use different ways of intercepting keys and of preventing other apps from intercepting keys. It is very messy.
If we implement custom key bindings with WinAPI it will lead to hundreds of conflicts with other apps and it will spoil the user experience.

On the contrary Oblita Interception Driver truly intercepts key presses. 99.9% of the apps including _Windows itself_ won't even notice that you pressed a key if it is intercepted by Oblita.
A single reboot is better than constantly fighting conflicting apps.

### EyeTrackerMouse is laggy sometimes. How to fix that?

This happens when you 100% load all the cores of your CPU. Most users will never face this issue.
It appears only when you are doing something special (e.g. Chromium compilation).

Tobii services has **Normal** process priority by default. When there are heavy-load tasks with higher-than-normal priority Tobii stops handling you eyes position and EyeTrackerMouse freezes.

**Changing process priorities may have unexpected consequences. Do it on your own risk.**

To address the issue set **High** process priority for:

1. All processes starting from Tobii
2. EyeTrackingMouse.exe

You can do it in _Windows Task Manager->Details_, but this is a temporary solution.
For a permanent solution install _System Explorer_ and set priorities with enabled _Permanent_ checkbox.
