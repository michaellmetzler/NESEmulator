# NES Emulator

The emulator was built in Unity 6000.5.0a4

The main scene is NESEmulator, and there is an equivalent Debug scene.

You can load a ROM by starting the scene and selecting the Emulator in the hierarchy.
There are buttons to Load and Reset the emulator.

All of the code live in the Assets/Scripts folder, with the emulator code living inside the NESEmulator folder.
The NES emulator itself is built using the .NET Standard libraries, no Unity libraries.

The emulator is built with a "catch up", which means that the CPU will execute one instruction at a time.
The PPU and APU will then run for the appropriate number of cycles based on the instruction that was run.
This isn't cycle accurate, but generally is good enough.

CPU implements all offical opcodes.
PPU fully implemented.
APU currently being implemented
Input is fully implmemented for the standard controller.