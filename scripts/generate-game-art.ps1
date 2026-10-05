param([string]$GameDir, [string]$Icon, [string]$Logo)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path $PSScriptRoot -Parent
if (-not $GameDir) { $GameDir = Join-Path $root 'Celeste' }
if (-not $Icon) { $Icon = @('art\icon.png', 'art\icon.jpg') | ForEach-Object { Join-Path $root $_ } | Where-Object { Test-Path $_ } | Select-Object -First 1 }
if (-not $Logo) { $Logo = Join-Path $root 'art\logo.png' }
$splash = Join-Path $GameDir 'Content\Graphics\SplashScreen.png'

$out = Join-Path $root 'src\Celeste.Android\GameArt'
Remove-Item -Recurse -Force $out -ErrorAction SilentlyContinue
$iconRes = Join-Path $out 'icon\res'
$artRes = Join-Path $out 'art\res'

function Save-Canvas([int]$w, [int]$h, [string]$path, [object[]]$layers, [string]$format = 'png') {
	New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
	$bmp = New-Object System.Drawing.Bitmap $w, $h
	$g = [System.Drawing.Graphics]::FromImage($bmp)
	$g.InterpolationMode = 'HighQualityBicubic'
	$g.PixelOffsetMode = 'HighQuality'
	foreach ($layer in $layers) {
		$g.DrawImage($layer.Image, $layer.Dest, $layer.Src, 'Pixel')
	}
	if ($format -eq 'jpg') {
		$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -eq 'image/jpeg'
		$params = New-Object System.Drawing.Imaging.EncoderParameters 1
		$params.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), 88L
		$bmp.Save($path, $codec, $params)
	}
	else {
		$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
	}
	$g.Dispose(); $bmp.Dispose()
}
function Layer($image, $src, [int]$x, [int]$y, [int]$w, [int]$h) {
	@{ Image = $image; Src = $src; Dest = (New-Object System.Drawing.Rectangle $x, $y, $w, $h) }
}
function Rect([int]$x, [int]$y, [int]$w, [int]$h) { New-Object System.Drawing.Rectangle $x, $y, $w, $h }

$iconImg = $null
if ($Icon) {
	$iconImg = [System.Drawing.Image]::FromFile((Resolve-Path $Icon))
	$iconSrc = Rect 0 0 $iconImg.Width $iconImg.Height
}
elseif (Test-Path $splash) {
	$iconImg = [System.Drawing.Image]::FromFile($splash)
	$iconSrc = Rect 700 0 480 480
}
if ($iconImg) {
	try {
		$densities = @{ 'mdpi' = 108; 'hdpi' = 162; 'xhdpi' = 216; 'xxhdpi' = 324; 'xxxhdpi' = 432 }
		foreach ($d in $densities.Keys) {
			$px = $densities[$d]
			$inset = [int]($px / 6)
			Save-Canvas $px $px "$iconRes\mipmap-$d\ic_celeste_bg.png" @(Layer $iconImg $iconSrc 0 0 $px $px)
			Save-Canvas $px $px "$iconRes\mipmap-$d\ic_celeste_fg.png" @(Layer $iconImg $iconSrc $inset $inset ($px - 2 * $inset) ($px - 2 * $inset))
			$legacy = [int]($px * 48 / 108)
			Save-Canvas $legacy $legacy "$iconRes\mipmap-$d\ic_celeste.png" @(Layer $iconImg $iconSrc 0 0 $legacy $legacy)
		}
		New-Item -ItemType Directory -Force "$iconRes\mipmap-anydpi-v26" | Out-Null
		@'
<?xml version="1.0" encoding="utf-8"?>
<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
	<background android:drawable="@mipmap/ic_celeste_bg" />
	<foreground android:drawable="@mipmap/ic_celeste_fg" />
</adaptive-icon>
'@ | Set-Content "$iconRes\mipmap-anydpi-v26\ic_celeste.xml" -Encoding utf8
	}
	finally { $iconImg.Dispose() }
}

if (Test-Path $splash) {
	$img = [System.Drawing.Image]::FromFile($splash)
	try {
		Save-Canvas 1920 1080 "$artRes\drawable-nodpi\celeste_art.jpg" @(Layer $img (Rect 0 0 $img.Width $img.Height) 0 0 1920 1080) 'jpg'
	}
	finally { $img.Dispose() }
}
if (Test-Path $Logo) {
	$logoImg = [System.Drawing.Image]::FromFile((Resolve-Path $Logo))
	try {
		$w = [Math]::Min(720, $logoImg.Width)
		$h = [int]($logoImg.Height * $w / $logoImg.Width)
		Save-Canvas $w $h "$artRes\drawable-nodpi\celeste_logo.png" @(Layer $logoImg (Rect 0 0 $logoImg.Width $logoImg.Height) 0 0 $w $h)
	}
	finally { $logoImg.Dispose() }
}

if (Test-Path $out) { Get-ChildItem $out -Recurse -File | ForEach-Object { $_.FullName.Replace("$out\", '') } }
