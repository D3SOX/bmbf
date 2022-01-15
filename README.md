# BMBF 2

## Repository Info

### Projects
- `BMBF` - The main BMBF project, which produces the Android APK for installation onto the Quest.
- `BMBF.Backend` - Contains the code for the BMBF backend, uses abstractions to interact with android-specific APIs. 
- `BMBF.Desktop` - A wrapper around BMBF for testing locally without an emulator. Allows BMBF to run directly on ones PC.
- `BMBF.DiffGenerator` - Automatically downloads Beat Saber versions and computes diffs for downgrading.
- `BMBF.ModManagement` - Abstractions which allow multiple mod types to be handled by BMBF.
- `BMBF.Patching` - Convenient API for patching and signing APKs, with manifest changes.
- `BMBF.QMod` - Implementation of [QMOD](https://github.com/Lauriethefish/QuestPatcher.QMod) loading and installation.
- `BMBF.QMod.Tests` - Tests for QMOD management.
- `BMBF.Resources` - C# classes for loading the JSON files in the [resources repo](https://github.com/BMBF/resources).

### Build requirements
- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- .NET 6 Android workload. To install, first install .NET 6, then run `dotnet workload install android` as administrator.

## Development

When testing BMBF during development, you have 3 options. The quest, an Android emulator, or directly on your PC.

### Using the Quest
1. Open `BMBF.sln` in the IDE of your choice (e.g. Visual Studio or Rider). 
2. Plug your Quest into your PC (make sure that you have USB debugging setup)
3. Run the project using the run/debug buttons in your IDE. This will automatically build, install, and run BMBF.

### Setting up an Emulator
1. Open the AVD manager in your IDE.
2. Create a new virtual device.
3. Choose an Android TV (720p) device, and make sure that you use an image with an API level of at least 29, ideally 30.
4. Select the device when running the BMBF project.
5. Note that you can forward the ports for BMBF to your PC by running `adb forward tcp:50005 tcp:50005`

### Running directly on your PC
1. Navigate to `./BMBF.Desktop`
2. Make sure that the working directory for the debugger in your IDE is the `BMBF.Desktop` directory - otherwise the wrapper won't be able to find the files that it needs.
3. Run the project in your IDE, or execute `dotnet run`.
4. BMBF will serve to `http://localhost:
50006`.

Some notes:
- Files
- The "beat saber installation" is simulated by an APK located in `./BMBF.Desktop/Device/BeatSaber.apk`. Creating or deleting this file is akin to installing or uninstalling Beat Saber.


## Creating a releasable APK
1. Navigate to the `./BMBF` project on the command line.
2. Execute `dotnet publish -c Release`.
3. The APK will be located at `./BMBF/bin/Release/net6.0-android/android-arm64/publish/com.weareneutralaboutoculus.BMBF-Signed.apk`

