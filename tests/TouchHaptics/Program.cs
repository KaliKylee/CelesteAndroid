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
