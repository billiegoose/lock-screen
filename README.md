# Transparent Lock Screen for Windows
(also a transparent screensaver)

It's a screensaver with 1% opacity. Unlike most screensavers, it doesn't end on mouse movement.
In fact, it blocks mouse clicks and keyboard events until you kill the screensaver with the
keyboard shortcut `Ctrl + L`. Although `Ctrl+Alt+Delete` isn't blocked, launching the Task Manager
is ineffective because the screensaver steals the focus again, so you aren't able to use the Task Manager,
just look at it wistfully. The last thing it does before it exits is call LockWorkStation(),
so if someone tries to use your computer, even after they unlock the screensaver with Ctrl + L
they will be presented with the Windows lockscreen with the Windows password prompt.
None of your co-workers will be pranking you now!

The code itself is a derivative of [Write a Screensaver that Actually Works](http://www.codeproject.com/Articles/14081/Write-a-Screensaver-that-Actually-Works),
with some of the guts ripped out to simplify it.

## Known issues
Windows 10 appears to have issues with transparent screensavers. (Issue [#2](https://github.com/wmhilton/lock-screen/issues/2#issuecomment-328207687))

## Download
There is a [compiled version available](https://github.com/wmhilton/lock-screen/releases).

## Current Status
I am not actively working on this project now that I telecommute and have no physically copresent coworkers trying to prank me,
but I will gladly accept pull requests.

## To Use Manually
Set up a trigger of some sort (say with AutoHotKey) to run "InvisibleLockScreen.scr /s".

## To Use As Screen Saver
Copy bin\Release\InvisbleLockScreen.scr to C:\Windows\SysWOW64.
In Screen Saver Settings, pick "InvisibleLockScreen" as your screensaver.
You can leave "On resume, display logon screen" unchecked.

## Similar Software
There are programs out there (e.g. Pro Key Lock, Transparent Screen Lock) that can lock your computer's keyboard/mouse while still showing the running applications, which can be very useful. 
However they rely on hacky techniques like intercepting Win32 Messages and they charge money. 
Pro Key Lock, for instance, freaked out when I turned on Sticky Keys for my RSI and I had to forcefully reboot my computer. 

After that, I felt there had to be a simpler way to do it that took advantage of Windows native lock screen abilities. So
I made this in about 3 hours.

# License
MIT
