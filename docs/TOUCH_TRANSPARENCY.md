# Button transparency

Options → Button transparency opens a slider and a live preview.

- 0% transparency = solid buttons; 100% = invisible buttons that still accept touch.
- Save stores the value in the existing `touch_opacity` preference; Cancel/back leaves the saved value unchanged.
- The setting is applied when starting the game. The controls editor uses the same preference.
- Controls with an individual opacity keep their override. Select “Use global opacity” in the controls editor to include them.
- Button opacity now maps linearly: 45% opacity renders at 45%, rather than the previous 90%. Existing stored percentages are preserved.
- The controls editor keeps a faint global preview at very low opacity so controls can still be positioned.

## Validation status

An ARM64 Release APK containing this change and the dynamic joystick was compiled and signed successfully with .NET 10 Android in the fork's GitHub Actions build. Game assets are not bundled. Source diff checks pass. Physical-device validation is pending.

Device checks after building:
1. Move the slider to 0%, 50%, 100% and verify the preview.
2. Save, reopen the dialog and restart the app: the percentage should persist.
3. Move the slider then Cancel/back: the saved percentage must stay unchanged.
4. Start the game and check the standard and custom buttons, including while pressed.
5. Check an individual opacity override and “Use global opacity”.
6. Verify the dialog scrolls in landscape on the phone.
