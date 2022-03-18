# BMBF 2

## Repository Info

### Projects

- `BMBF` - The main BMBF project, which produces the Android APK for installation onto the Quest.
- `BMBF.Backend` - Contains the code for the BMBF backend, uses abstractions to interact with android-specific APIs.
- `BMBF.Backend.Tests` - Unit tests for the backend
- `BMBF.Desktop` - A wrapper around BMBF for testing locally without an emulator. Allows BMBF to run directly on one's PC.
- `BMBF.DiffGenerator` - Automatically downloads Beat Saber versions and computes diffs for downgrading.
- `BMBF.ModManagement` - Abstractions which allow multiple mod types to be handled by BMBF.
- `BMBF.Patching` - Convenient API for patching and signing APKs, with manifest changes.
- `BMBF.QMod` - Implementation of [QMOD](https://github.com/Lauriethefish/QuestPatcher.QMod) loading and installation.
- `BMBF.QMod.Tests` - Unit tests for QMOD management.
- `BMBF.Resources` - C# classes for loading the JSON files in the [resources repo](https://github.com/BMBF/resources).
- `BMBF.WebServer` - Simple, lightweight web framework for the BMBF API.

### Build requirements

- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- .NET 6 Android workload. To install, first install .NET 6, then run `dotnet workload install android` as administrator.
- JDK 11
- [NuGet CLI](https://www.nuget.org/downloads) on PATH.

### Containerised build environment

Alternatively, if you have docker installed, you can use the containerised build environment. This is what we use in CI and it is guaranteed to reliably work.
Just mount the project directory as a docker volume and expose the right ports when debugging locally.

```sh
docker pull registry.bmbf.dev/unicorns/bmbf/buildenv
docker run -it -v "$(pwd):/bmbf/src" -p "50006:50006" registry.bmbf.dev/unicorns/bmbf/buildenv
```

As of now, you need to set two environment variables (this will be removed soon).

`GITHUB_USERNAME` to your github username.

`GITHUB_KEY` to a github personal access token with the `read:packages` permission.

## Development

When testing BMBF during development, you have 2 options. The quest, or running BMBF directly on your PC.

### Using the Quest

1. Open `BMBF.sln` in the IDE of your choice (e.g. Visual Studio or Rider).
2. Plug your Quest into your PC (make sure that you have USB debugging setup)
3. Run the project using the run/debug buttons in your IDE. This will automatically build, install, and run BMBF.

### Running directly on your PC

1. Navigate to `./BMBF.Desktop`
2. Make sure that the working directory for the debugger in your IDE is the `BMBF.Desktop` directory - otherwise the wrapper won't be able to find the files that it needs.
3. Run the project in your IDE, or execute `dotnet run`.
4. BMBF will serve to `http://localhost:50006`.

Some notes:

- Files in BMBF.Desktop will be located in the `./BMBF.Desktop/Device` directory - note that the directories don't completely reflect the Quest's filesystem, to make things easier to find.
- The "beat saber installation" is simulated by an APK located in `./BMBF.Desktop/Device/BeatSaber.apk`. Creating or deleting this file is akin to installing or uninstalling Beat Saber.

## Browsing API endpoints

API endpoints can be viewed in swagger at the endpoint `/api/swagger` (only applies in the `Debug` configuration)

## Creating a releasable APK

1. Navigate to the `./BMBF` project on the command line.
2. Execute `dotnet publish -c Release`.
3. The APK will be located at `./BMBF/bin/Release/net6.0-android/android-arm64/publish/com.weareneutralaboutoculus.BMBF-Signed.apk`
