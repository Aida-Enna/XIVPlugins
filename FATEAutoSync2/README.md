# FATEAutoSync

Automatically synchronizes the level of your character whenever you join a FATE in Final Fantasy XIV.

`/fateautosync` to toggle without uninstalling the plug-in.

This is very basic, but it works. I don't know if it's bad for performance, really, I'm not used to Dalamud or C# at all for that matter. Even doing something like reading a byte from a memory address is exciting to me...

It will break whenever the game updates, I assume, but finding the one address needed to know whether or not we are currently participating in a FATE is very easy to find.

Maybe this is overkill, since I hear it can be done with Triggernometry most likely. But I wanted to have fun with C# for 5 minutes. (even though a big chunk of this comes from [QoLBar](https://github.com/UnknownX7/QoLBar) for chat command execution, and other plugins for knowing finding out how to read memory)

## Screenshot

![](https://user-images.githubusercontent.com/3979239/149154647-893be983-0b55-4a1a-b618-49efa6dd7a4d.png)
