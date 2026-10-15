# Touch vibration

Open **Options → Vibration** in the launcher. Preferences are saved when you tap **Save**; **Cancel** keeps the previous values. A scrollable dialog accommodates landscape screens.

- **Enable vibration:** master switch for all touch-control pulses.
- **Vibrate on buttons:** press and release, press only, or release only. Applies to the five built-in buttons, custom buttons, and D-pad arrows.
- **Intensity:** 0–100%, default 50%. Zero is silent. Supported motors use amplitude control; other devices vary pulse duration and show an explanatory hint.
- **Joystick: ticks every 45°:** independent switch for fixed/dynamic joystick axes. The button press/release mode does not affect axis ticks; the master switch and intensity do.
- **Press and release to test:** previews the selected button behavior and strength before saving.

Android touch-feedback settings remain authoritative. Devices without a vibration motor display an availability message and disable the preview. Pausing the game cancels touch vibration.

Settings travel to the separate game process as Intent extras to avoid stale cross-process SharedPreferences caches. `HapticPolicy` validates stored/received values and filters events; `TouchVibration` performs short pulses with touch-feedback audio attributes.

Run `dotnet run --project tests/TouchHaptics -c Release` for direction and policy checks. Physical-device gameplay, perceived intensity, settings persistence, and preview behavior still require a phone test.

Suggested phone checks: try each button mode with a long hold, test 0/50/100%, turn off joystick ticks, disable the master switch, save and relaunch, change settings and cancel, and pause/resume. Check both joystick styles and custom buttons.
