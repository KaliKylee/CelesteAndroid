using System;
using CelesteAndroid;

static bool At(StickHaptics s, double degrees) => s.Update((float)Math.Cos(degrees * Math.PI / 180), (float)Math.Sin(degrees * Math.PI / 180));
static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
var stick = new StickHaptics();
Check(!stick.Update(0, 0), "neutral is silent");
for (int i = 0; i < 8; i++)
{
    stick.Update(0, 0);
    Check(At(stick, i * 45), $"axis {i} ticks");
    Check(!At(stick, i * 45), "holding does not repeat");
    Check(!At(stick, i * 45 + 4), "small jitter is silent");
    Check(!At(stick, i * 45 - 4), "cross-axis jitter is silent");
}
stick.Update(0, 0);
Check(!At(stick, 10), "off-axis engagement is silent");
Check(At(stick, -10), "fast clockwise crossing detected");
stick.Update(0, 0);
At(stick, -10);
Check(At(stick, 10), "fast counterclockwise crossing detected");
stick.Update(0, 0);
At(stick, 170);
Check(At(stick, -170), "wrap crossing detected");
stick.Update(0, 0);
At(stick, -170);
Check(At(stick, 170), "reverse wrap crossing detected");
stick.Update(0, 0);
Check(At(stick, 0), "reengagement ticks");
Check(!At(stick, 15), "leaving axis is silent");
Check(At(stick, 0), "returning after leaving rearms");
for (int i = 1; i <= 16; i++) Check(At(stick, i * 45), "successive directions tick");
Console.WriteLine("All joystick haptic checks passed.");

// Modes must filter button edges without muting the separately configured joystick.
for (int mode = 0; mode < 3; mode++)
{
    var policy = new HapticPolicy(true, mode, 50, true);
    Check(policy.ShouldPlay(1) == (mode != 2), "press mode filtering");
    Check(policy.ShouldPlay(2) == (mode != 1), "release mode filtering");
    Check(policy.ShouldPlay(3), "joystick independent of button mode");
    Check(!policy.ShouldPlay(99), "unknown event ignored");
    for (int kind = 1; kind <= 3; kind++)
    {
        Check(!new HapticPolicy(false, mode, 100, true).ShouldPlay(kind), "master off mutes everything");
        Check(!new HapticPolicy(true, mode, 0, true).ShouldPlay(kind), "zero intensity is silent");
    }
}
Check(!new HapticPolicy(true, 0, 50, false).ShouldPlay(3), "joystick toggle");
Check(new HapticPolicy(true, 99, -1, true).Mode == 0, "invalid mode defaults safely");
Check(new HapticPolicy(true, 0, -1, true).Intensity == 0, "negative intensity clamped");
Check(new HapticPolicy(true, 0, 101, true).Amplitude == 255, "maximum amplitude clamped");
int previousAmplitude = 0;
for (int strength = 1; strength <= 100; strength++)
{
    var policy = new HapticPolicy(true, 0, strength, true);
    Check(policy.Amplitude > previousAmplitude && policy.Amplitude <= 255, "strength increases amplitude");
    Check(policy.DurationMs(1, true) == 12, "amplitude-capable pulse stays short");
    Check(policy.DurationMs(1, false) >= 1 && policy.DurationMs(1, false) <= 12, "fallback stays bounded");
    previousAmplitude = policy.Amplitude;
}
Console.WriteLine("All vibration mode, master switch, intensity, and fallback checks passed.");
