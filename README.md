# Transparent Lock Screen for Windows 
(also a transparent screensaver)

It's a screensaver with 1% opacity. Unlike most screensavers, it doesn't end on mouse movement, just on clicks or
keyboard presses. And the last thing it does before it exists is call LockWorkStation() so if someone
tries to use your computer, they'll be presented with the Windows lockscreen.
None of your co-workers will be pranking you now!

_Currently a derivative of [Write a Screensaver that Actually Works](http://www.codeproject.com/Articles/14081/Write-a-Screensaver-that-Actually-Works),
however I am slowly ripping out the guts to simplify it._

## Current Status
It's a personal project. Feel free to download the source & compile it.

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

