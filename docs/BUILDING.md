# Building CelesteAndroid

This guide builds the APK from source on **Windows** (PowerShell). The scripts are PowerShell, but every step is plain `dotnet`/CMake/NDK and can be adapted to Linux/macOS.

## 1. Toolchain

| Tool | Version used | Notes |
|---|---|---|
| .NET SDK | 10.0.x | `winget install Microsoft.DotNet.SDK.10` |
| .NET Android workload | 36.1 | `dotnet workload install android` |
| JDK | 17 (Microsoft OpenJDK) | `winget install Microsoft.OpenJDK.17` |
| Android SDK | platform 36, build-tools 36.0.0, platform-tools | via `cmdline-tools` |
| Android NDK | **r27d** (27.3.13750724) | LTS; used for SDL3/FNA3D/FAudio |
| CMake + Ninja | CMake 3.24+ / Ninja | any recent version on `PATH` |
| 7-Zip | any | only to extract the FMOD Windows installer (desktop host) |

Environment variables (user level):

```
JAVA_HOME         = C:\Program Files\Microsoft\jdk-17...
ANDROID_HOME      = C:\Android\sdk
ANDROID_NDK_HOME  = C:\Android\sdk\ndk\27.3.13750724
```

> Recent versions of `sdkmanager` forward to the new **Android CLI** (`android.exe sdk install ...`), which may crash *on exit* (0xC0000409) after installing correctly. Check the installed folders rather than trusting the exit code.

## 2. Clone

```powershell
git clone --recursive https://github.com/BelmanteGu/CelesteAndroid
```

`external/FNA` (with FNA3D, FAudio, SDL3-CS...) and `external/SDL` (release-3.4.16) are submodules.

## 3. Files you have to supply

These are **not** in the repository and must never be committed (they're in `.gitignore`):

| Path | What |
|---|---|
| `Celeste/` | Your Celeste PC copy, **FNA build** (`Celeste.exe`, `FNA.dll`, `Content/`). Steam: *opengl* beta. itch.io: Linux zip. Used for compiling the patch module and, optionally, for personal builds. |
| `fmod/libs/android-arm64/` | `libfmod.so`, `libfmodstudio.so`, `fmod.jar` from the FMOD Engine **1.10.14** Android package (`api/lowlevel/lib/arm64-v8a`, `api/studio/lib/arm64-v8a`, `api/lowlevel/lib/fmod.jar`) |
| `fmod/libs/win-x64/` | *(desktop host only)* `fmod64.dll` from the Windows package, copied as **both** `fmod.dll` and `fmod64.dll`, plus `fmodstudio64.dll` copied as `fmodstudio.dll` |
| `art/icon.png` (or `.jpg`) | *(optional)* square image for the app icon, used by every build. Without it, personal builds crop the key art and public builds use the original mountain icon. |
| `art/logo.png` | *(optional, personal builds)* logo shown in the launcher instead of the text title |

Why 1.10.14: Celeste's `.bank` files were built with FMOD Studio 1.10, and the game's C# FMOD wrapper matches that API.

## 4. Native libraries

```powershell
scripts\build-natives-android.ps1          # -> natives\android-arm64\libSDL3.so, libFNA3D.so, libFAudio.so
```

API 26, arm64-v8a, `ANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON` (16 KB page alignment for Android 15+).

## 5. APK

```powershell
scripts\build-release.ps1                  # public APK (no official art) -> out\
scripts\build-release.ps1 -Personal        # official icon/logo from YOUR game files (don't share)
scripts\build-release.ps1 -Personal -EmbedGame   # also bundles your game (~860 MB; don't share)
```

Signing: if `%USERPROFILE%\.celeste-android\keystore.env` exists it's used (`CELESTE_KEYSTORE`, `CELESTE_KEYSTORE_ALIAS`, `CELESTE_KEYSTORE_PASS`); otherwise the debug key is used. Keep the same key across builds, or Android won't let you update without uninstalling (which deletes saves).

## 6. Desktop test host (optional)

`src/Celeste.Desktop` runs the exact same patched `Celeste.dll` on Windows x64. It's the fastest way to test patches:

```powershell
scripts\fetch-fnalibs.ps1                  # SDL3/FNA3D/FAudio x64 -> natives\win-x64
dotnet run --project src\Celeste.Desktop    # finds .\Celeste automatically, or use --game <dir>
```

It behaves like the Android build (`Platform = "Android"`, saves go to `bin\...\userdata`), so your real PC saves are never touched.

## 7. Development loop on a device

```powershell
scripts\deploy-android.ps1 -Content        # first time: also pushes Content/ (1 GB)
scripts\deploy-android.ps1                 # afterwards: patch + debug APK + patched dll
adb logcat -s CelesteAndroid DOTNET SDL SDL/GPU fmod
```

The debug build is `run-as`-able, so the script copies files straight into the app's private storage. To regenerate the patch and background on the device without re-importing:

```
adb shell am start -n org.celesteandroid.celeste/.LauncherActivity --ez repatch true
```

To force OpenGL ES: `--es driver OpenGL` on `GameActivity`, or the *Graphics* toggle in the launcher.

## 8. Building in GitHub Actions

`.github/workflows/build-apk.yml` builds the **public** APK (run it from the *Actions* tab, or push a `v*` tag to also attach the APK to a release). Celeste.exe and FMOD are proprietary, so they are downloaded from private links stored as repository secrets:

| Secret | What |
|---|---|
| `CELESTE_ZIP_URL` | Direct link to a `.zip` (or the bare `Celeste.exe`) of your FNA build. Only `Celeste.exe` is used. |
| `FMOD_ZIP_URL` | Direct link to a `.zip` with `libfmod.so`, `libfmodstudio.so` (arm64-v8a) and `fmod.jar` from FMOD 1.10.14. The official Android package zip also works. |
| `KEYSTORE_BASE64`, `KEYSTORE_ALIAS`, `KEYSTORE_PASSWORD` | *(optional)* release keystore (`base64 -w0 my.keystore`). Without it the debug key is used. |

The workflow caches the native libs (SDL3, FNA3D, FAudio), so only the first run (or a submodule update) pays for the CMake build.
