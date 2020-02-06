# Rabbit Patcher
### A patching tool for Oh Jeez, Oh No, My Rabbits Are Gone
Run the [patcher](https://github.com/Shana6/rabbit-patcher/releases/latest) using
```
.\RMP.exe <path to data.win> [all/everything/actions...]
```
## Features:
- Speedbun Mode 
	- Framework for other patches, adds Speedbun menu in main menu
    - 
- Speedrun Clock
    - Adds a clock to the top left of the screen 
	- Only works in speedbun mode
- Intro Skipping
    - Skips the intro at the start of the game
	- Only works in speedbun mode
- Framecap Remover
	- Press "P" to toggle the framerate max between 60 and 1000
- Randomly Colored Bunnies
	- Every time a room starts, every bunny is tinted a random hue.
- Decompiler wrapper
	- Dumps all code to the folder where the data.win file is.
- Doesn't overwrite the original data.win
	- Places it in a file named data.win.orig in the same folder as the original data.win
## Actions
Actions are used to select patches or perform different less important actions (like decomp).
There are actions/patches that are not related to speedrunning, like `color` which just changes the colors of the rabbits to any random color on every room (re)start.
They can still be accessed as normal actions or through the `all` action, but aren't ran when you don't provide any actions in the command.
