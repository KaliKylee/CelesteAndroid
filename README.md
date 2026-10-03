# CelesteAndroid

**A native Android port of Celeste (2018), running on your own copy of the PC game.**

CelesteAndroid is a launcher + runtime that takes the files of the Celeste PC version you own and runs them natively on Android: .NET on ARM64, FNA + SDL3, Vulkan (or OpenGL ES) graphics and FMOD audio. It doesn't emulate anything and it doesn't use a browser/WebAssembly.

> [!IMPORTANT]
> **No game files are included.** This project doesn't include Celeste or any of its assets. You need a legally owned copy of Celeste for PC. The launcher imports it from your device, the same model PortMaster and other community ports use.

## Features

- **Native**: the game runs on the .NET runtime for ARM64, with FNA, FNA3D and SDL3 built for Android.
- **Vulkan by default** (SDL_GPU), with OpenGL ES as a fallback you can pick in the launcher.
- **Full audio**: FMOD Studio 1.10.14, the same version the game's sound banks were built with.
- **Launcher**: pick your game folder or `.zip` and the app copies it, patches it and gets it ready to play.
- **Bring your saves**: *Import saves* copies your `.celeste` save files (e.g. the `Saves` folder from your PC) into the game.
- **Wide screens**: the game keeps its native 16:9 image and the side bars show a blurred version of the game's key art instead of plain black.
- **Your files stay yours**: the original `Celeste.exe` is never modified. A patched copy is generated on the device with [MonoMod](https://github.com/MonoMod/MonoMod), the same tooling the [Everest](https://everestapi.github.io/) mod loader uses.

## Requirements

| | |
|---|---|
| Device | Android 8.0+ (API 26), **ARM64** (arm64-v8a) |
| Storage | ~1.2 GB free (the game's `Content` folder is ~1.1 GB) |
| Game | **Celeste for PC, FNA build** (see below) |
| Input | **Touch screen** (on-screen joystick + Jump/Dash/Grab/Pause buttons) or a game controller (Bluetooth or USB); the overlay hides itself when a controller is connected. |

### Which version of Celeste?

You need the **FNA build** of the PC version. Its folder contains `Celeste.exe`, **`FNA.dll`** and `Content/`.

- **Steam**: Library → right-click Celeste → *Properties* → *Betas* → pick **`opengl`**. After the update, copy the whole game folder (*Manage → Browse local files*).
  The default Steam build on Windows uses XNA and **won't work** (there's no `FNA.dll`).
- **itch.io**: the **Linux `.zip`** already is the FNA build. You can import the `.zip` directly.

Tested with Celeste **1.4.0.0**.

## Installing

1. Download `CelesteAndroid-<version>.apk` from [Releases](../../releases) and install it.
2. Copy your Celeste folder (or the itch.io `.zip`) to your phone, for example to `Download/`.
3. Open **Celeste** and tap **Open game files** (or **Import .zip**) and select it.
4. Wait for the import (copy + patch, about 1–2 minutes), then tap **PLAY**.

Your saves live in the app's private storage, and uninstalling the app deletes them. To bring your PC progress over, copy the `Saves` folder from your PC game folder to your phone and use **Import saves**. Your current saves are backed up to `Backups/` first.

## Building from source

Short version (Windows; see [docs/BUILDING.md](docs/BUILDING.md) for details):

```powershell
git clone --recursive https://github.com/BelmanteGu/CelesteAndroid
cd CelesteAndroid
# 1. Put your Celeste (FNA build) in .\Celeste\ and the FMOD 1.10.14 Android libs in .\fmod\libs\android-arm64\
# 2. Native libs (SDL3, FNA3D, FAudio) for arm64 with the Android NDK:
scripts\build-natives-android.ps1
# 3. APK:
scripts\build-release.ps1              # -> out\CelesteAndroid-<version>.apk
```

Useful scripts:

| Script | What it does |
|---|---|
| `scripts/build-natives-android.ps1` | Builds SDL3, FNA3D and FAudio for Android (NDK r27) |
| `scripts/build-release.ps1` | Release APK. `-Personal` adds the official icon/logo taken from *your* game files; `-EmbedGame` puts your game inside the APK. **Personal APKs must never be shared.** |
| `scripts/deploy-android.ps1` | Development loop: patch, install the APK and push files over `adb` |
| `scripts/fetch-fnalibs.ps1` | Downloads the x64 fnalibs for the desktop test host |
| `scripts/generate-game-art.ps1` | Extracts the icon/key art from your game files (for personal builds) |

## How it works

```
Launcher (Android UI)          Game process (":game")
 ├─ import folder/.zip          ├─ SDLActivity (SDL3 Java) → GameActivity.Main() on the SDL thread
 ├─ MonoMod: Celeste.exe  ──►   ├─ loads the patched Celeste.dll and calls Celeste.Main()
 │   → patched Celeste.dll      ├─ FNA (C#) → FNA3D (Vulkan/GLES) + SDL3
 └─ blurred side-bar art        └─ FMOD 1.10.14 (Java + .so)
```

The patches are small and focused: a Steamworks stub, SDL2 → SDL3 shims, `GetEntryAssembly` fixes, an Android GC fallback and the pillarbox background renderer. The full story (every problem and how it was solved) is in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Status

| | |
|---|---|
| ✅ | Boots to the title screen and plays with a controller (Galaxy S23, Snapdragon 8 Gen 2, Android 16) |
| ✅ | Vulkan and OpenGL ES, 60 FPS, FMOD audio, pause/resume |
| ✅ | Import from a folder or `.zip`, patching on the device |
| 🚧 | **On-screen touch controls** (FNA only reads the first of the several touch devices SDL reports) |
| ✅ | Importing `.celeste` saves |
| 🚧 | Everest / mods |

Tested on one device so far. Reports from other phones (especially Mali/Exynos/Tensor GPUs) are very welcome.

## Legal

- Celeste © Maddy Makes Games Inc. This project is **not affiliated with or endorsed by** Maddy Makes Games, Extremely OK Games or Firelight Technologies.
- This repository contains **no** game code or assets. The game is loaded from the user's own copy at runtime.
- The app icon of the released APK uses Madeline artwork from Celeste (© Maddy Makes Games), as fan ports commonly do. The icon file isn't in this repository. Builds from source use the original mountain icon unless you provide `art/icon.png`.
- **FMOD**: the released APK includes the FMOD Studio 1.10.14 runtime libraries. *FMOD Studio by Firelight Technologies Pty Ltd.* FMOD isn't open source and is **not** covered by this project's MIT license. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Credits

- [FNA](https://fna-xna.github.io/), FNA3D and FAudio by Ethan Lee (flibitijibibo) and contributors
- [SDL](https://libsdl.org/)
- [MonoMod](https://github.com/MonoMod/MonoMod) and [Mono.Cecil](https://github.com/jbevain/cecil)
- Prior art and inspiration: [Everest](https://github.com/EverestAPI/Everest), [celeste-wasm](https://github.com/MercuryWorkshop/celeste-wasm), [FNADroid](https://github.com/0x0ade/FNADroid) and JohnnyonFlame's [FNAPatches](https://github.com/JohnnyonFlame/FNAPatches)
- And of course, Celeste by Maddy Thorson, Noel Berry and the whole team. Go buy it if you haven't.

## License

The code in this repository is under the [MIT License](LICENSE). Third-party components keep their own licenses ([THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)).
