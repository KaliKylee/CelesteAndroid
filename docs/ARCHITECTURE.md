# Architecture and porting notes

This document covers how CelesteAndroid works and every non-obvious problem found while porting, so the next person (or the next port of an FNA game) doesn't have to rediscover them.

## Why this is possible at all

Celeste is a C# game built on **FNA** (an XNA 4 reimplementation). `Celeste.exe` isn't x86 machine code: it's .NET IL, which is architecture independent. So the port doesn't decompile or rewrite the game. It provides:

1. a .NET runtime on ARM64 (.NET 10 for Android, Mono runtime),
2. FNA (C#) plus its native dependencies compiled for Android (SDL3, FNA3D, FAudio),
3. FMOD, the audio middleware the game calls directly,
4. a small set of patches, applied with MonoMod to a copy of the user's `Celeste.exe`.

## Components

```
src/
  Celeste.Android/           .NET for Android app (launcher + game activity)
    LauncherActivity.cs      UI: key art, PLAY, import folder/.zip/saves, graphics toggle
    GameInstaller.cs         import (SAF folder, .zip, embedded assets) → MonoMod patch → blurred background
    GameActivity.cs          SDLActivity subclass, process ":game"; runs the game on the SDL thread
    CelesteLauncher.cs       loads the patched Celeste.dll and invokes Celeste.Main via reflection
    HelloGame.cs             minimal FNA game (stack smoke test; used if the game isn't installed)
  Celeste.Android.Patches/   MonoMod patch module (Celeste.Android.mm.dll), merged into Celeste.dll
  Celeste.Patcher/           wraps MonoModder: Celeste.exe + mm.dll → Celeste.dll (desktop and device)
  Celeste.Desktop/           Windows x64 host running the same patched dll (fast iteration)
  Steamworks.NET/            stub with the exact signatures Celeste references
external/FNA, external/SDL   submodules
scripts/                     natives build, release build, device deploy, art extraction
```

## Boot sequence on Android

1. **LauncherActivity** (main process). If the game isn't installed: the user picks a folder/.zip → `GameInstaller` copies `Celeste.exe` + `Content/` into `files/Celeste` (internal storage) → runs **MonoMod on the device** → `files/patched/Celeste.dll` → generates `files/background.png`.
2. **PLAY** starts **GameActivity** in a separate process (`:game`). Celeste and FNA keep a lot of static state, so every session gets a fresh process, and the process is killed when the activity finishes.
3. `GameActivity` extends SDL's `SDLActivity` (SDL's Java sources are compiled into the APK and bound to C# with `AndroidJavaSource Bind="true"`). It overrides:
   - `getLibraries()` → `SDL3`, `fmod`, `fmodstudio` (FMOD must be loaded before `FMOD.init`);
   - `main()` → runs managed code **directly on the SDLThread**, instead of SDL's `nativeRunMain`/`SDL_main`. FNA calls `SDL_SetMainReady` itself, so nothing is lost.
4. `CelesteLauncher.Run` passes configuration to the patched game through `AppContext` data (`CelesteAndroid.Platform`, `.PrefPath`, `.BackgroundPath`), overrides `APP_CONTEXT_BASE_DIRECTORY`, sets the working directory to the game folder, sets `Monocle.Engine.AssemblyDirectory` by reflection and invokes the private static `Celeste.Main(string[])`.

## The patches (`Celeste.Android.Patches`)

| Problem | Fix |
|---|---|
| `Main` calls `SteamAPI.Init()` / `RestartAppIfNecessary`; achievements and stats call Steamworks | `Steamworks.NET` stub assembly: same names and signatures (they must match the IL), all no-ops, `Init()` returns true |
| Celeste calls SDL**2** directly (`SDL_GetPlatform`, `SDL_GetPrefPath`), but current FNA runs on SDL**3** | `[MonoModLinkFrom]` relinks both to `SDLShim`: the platform comes from the host (`"Android"`), and the pref path points to the app's private storage. This also decides where saves go (`UserIO.GetSavePath`). |
| `Assembly.GetEntryAssembly()` is used to scan the game's own types (Tracker, Pooler, Overworld, Commands). Under a host the entry assembly is the host (or null on Android). | Relinked to return the Celeste assembly |
| `Engine.AssemblyDirectory` comes from `Assembly.Location` (empty when loaded on Android) | Set by reflection from the host |
| `GCSettings.LatencyMode = SustainedLowLatency` throws `PlatformNotSupportedException` on the Android Mono runtime | Relinked to a shim that sets it via reflection and ignores that exception. It *must* go through reflection: a direct call inside the shim would itself be relinked → infinite recursion. |
| `ErrorLog.Open()` launches the log with `Process.Start` | Replaced with a no-op (the log is still written) |
| 19.5:9 screens show black pillarbox bars | `Engine.RenderCore` replaced: draws `background.png` (blurred, darkened key art) full-screen, then fills the 16:9 game viewport with the clear color and renders the scene as usual |
| `Celeste.exe` is flagged 32-bit (`32BITREQUIRED`) | The patcher clears the flag (ILOnly, AnyCPU) |

`BinaryFormatter` (removed from modern .NET) appears in `Monocle.SaveLoad`, but nothing calls it.

## Native side

- **SDL 3.4.16**, **FNA3D** and **FAudio** are built with NDK r27d for arm64-v8a, API 26, with 16 KB page alignment. They depend only on system libraries (no `libc++_shared`).
- FNA3D includes both drivers. The default is **SDL_GPU → Vulkan** (Adreno 740: *Vulkan Conformance 1.3*). OpenGL ES can be forced with `FNA3D_FORCE_DRIVER=OpenGL`. It has to be set with `SDL_SetHint`: on Unix, .NET's `Environment.SetEnvironmentVariable` doesn't reach native `getenv`.
- SPIR-V goes straight to Vulkan. `SDL_shadercross` is optional, loaded at runtime only if present, and not needed.
- `FNADllMap` is inert on Android (no `.dll.config`), so `DllImport("SDL3")` resolves to `libSDL3.so` naturally.

## FMOD

- Celeste's banks need **FMOD Studio 1.10.x**. Version 1.10.14 is still downloadable from fmod.com ("Older" → "Unsupported"). The arm64 libs only depend on system libraries and are 64 KB aligned.
- On Android FMOD needs its Java part: `fmod.jar` is bound (`AndroidLibrary Bind="true"`), and `org.fmod.FMOD.init(context)` is called in `GameActivity.OnCreate`, after SDL's Java code has loaded `libfmod.so`.
- On Windows (desktop host), `fmodstudio64.dll` imports **`fmod64.dll`** by name, so that file must exist next to `fmod.dll` (which is the name the game's `DllImport("fmod")` looks for).

## On-device patching

MonoMod (and Mono.Cecil) run inside the launcher. The things that were needed:

- MonoMod needs **files**, not the .NET assembly store. So the APK carries `assets/patcher/`: the patch module, `FNA.dll`, `Steamworks.NET.dll` and **`MonoMod.Patcher.dll`** (it defines the `[MonoModPatch]` attributes the relinker must resolve). An MSBuild target adds them as assets after reference resolution.
- `ReadingMode.Deferred`: in `Immediate` mode Cecil resolves every custom attribute's types up front (for example `DebuggableAttribute`'s enum in `mscorlib`), and the .NET Framework reference assemblies don't exist as files on Android.

## Release build

- **No trimming, no AOT.** The game is loaded at runtime and uses reflection and `XmlSerializer` over the whole framework, including the .NET Framework facades (`mscorlib`, `System.Xml`...) that `Celeste.exe` references. AOT requires trimming in .NET for Android, so the release runs on JIT, like the debug build.
- Official art is generated by `scripts/generate-game-art.ps1` into `GameArt/` (git-ignored), in two independent parts:
  - `GameArt/icon`: the app icon, used by **every** build when present (`art/icon.png|jpg`, or a crop of the key art). The whole image sits in the central 72dp of the adaptive icon, so launcher masks never crop the face.
  - `GameArt/art`: the launcher background (the user's `Content/Graphics/SplashScreen.png`) and logo (`art/logo.png`), **personal builds only**. The public APK is built with `-p:UseGameArt=false` and shows the key art from the *imported* game instead.
  Resource *aliases* (`@mipmap/app_icon`, `@drawable/launcher_art`) switch between the generated art and the original fallbacks. `build-release.ps1` always builds clean and aborts if a public APK contains game files, the key art or the logo.

## Gotchas found along the way

- **adb-pushed files in `Android/data/<pkg>`** are visible to `run-as`, but the running app didn't see them. The fix was to use internal storage (`files/`), which also avoids FUSE overhead for 1 GB of content.
- **Debug APKs installed by hand** need `EmbedAssembliesIntoApk=true`. Otherwise Fast Deployment aborts with *"No assemblies found"*.
- **SDL Java bindings**: a few protected fields use package-private types. They're removed in `Transforms/Metadata.xml`.
- **FNA viewport after `ApplyChanges`**: on Android the internal viewport stays at the first backbuffer size (800×480). Celeste sets its own viewport every frame, so it isn't affected. `HelloGame` resets it explicitly.
- **Touch**: SDL reports **3 touch devices** on the S23, but FNA's `UpdateTouchPanelState` only polls device index 0, so touches never reach `TouchPanel`. This has to be fixed before on-screen controls.
- **fnalibs**: the old archive URL is gone. The dailies are GitHub Actions artifacts, fetched via nightly.link (`scripts/fetch-fnalibs.ps1`).

## Touch controls

`TouchControls.cs` (patch module). FNA's `TouchPanel` is skipped entirely because it polls only touch device 0:

1. Fingers are read straight from SDL3 (`SDL_GetTouchDevices` + `SDL_GetTouchFingers`, all devices, normalized 0..1 → backbuffer pixels).
2. `GamePadShim` relinks Celeste's calls to `GamePad.GetState(...)` and returns a synthetic `GamePadState` (stick, A, X+B, trigger, Start, D-pad derived from the stick for menus). A real controller, when connected, takes priority and hides the overlay.
3. `patch_Engine.RenderCore` calls `TouchControls.Draw` after the scene, drawing the overlay with a `SpriteBatch`.

Mapping: fixed stick at the bottom-left (the base never moves; touches in a wider outer zone around it also grab the stick); right side Jump → A, Dash → X+B, Grab → right trigger, top-right Pause → Start. Tunables are the static fields at the top of the class.

Launcher options: the "Options" button on the launcher opens a menu with **Edit controls** (opens `ControlsEditorActivity`), **Show FPS** and **Hide touch buttons**. The values live in the `launcher` SharedPreferences (`GameOptions`); because the game runs in the `:game` process, `LauncherActivity.Play` forwards them as Intent extras, `GameActivity.Main` copies them into `AppContext` and `TouchControls` reads them through `HostConfig.ShowFps` / `HideTouch` / `TouchOpacityPercent`. Hiding the buttons also disables touch input (the real gamepad state is returned), and the FPS counter is drawn by `TouchControls.Draw` even when the buttons are hidden.

Layout editor: `ControlsEditorActivity`, where each control can be dragged and resized, and where a 0–100% opacity slider (default 45%, same as `TouchControls.Opacity`) sets the transparency of all controls; it is saved with the layout (the editor preview never goes below 12% so the controls stay draggable). The result is saved as fractions of the screen in `files/touch_layout.txt` (lines `name=x,y,scale`); `TouchControls` reads it on startup via `HostConfig.TouchLayoutPath`, and no file means the default layout. Default positions are duplicated in `ControlsCanvas.SetDefaults` and `TouchControls.Center`/`StickBase` and must be kept in sync.

## Roadmap

- Touch controls: first version done (see "Touch controls"); still to do: layout editor, configurable keys
- Everest (mod loader) support
- More devices tested (Mali, Xclipse, Tensor)

## Orientation

The app is landscape-only. All three activities (`LauncherActivity`, `ControlsEditorActivity`, `GameActivity`) are declared `SensorLandscape` (so it is applied by the system before the first frame) and set it again at the top of `OnCreate`. Each also overrides `RequestedOrientation` and passes every request through `LandscapeLock.Coerce`, so when SDL asks for an orientation while creating the window (before the `SDL_ORIENTATIONS=LandscapeLeft LandscapeRight` hint set in `GameActivity.Main` takes effect) anything that is not landscape becomes `SensorLandscape`. No portrait value is used anywhere.
