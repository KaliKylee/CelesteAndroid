param(
	[switch]$Personal,
	[switch]$EmbedGame
)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src\Celeste.Android\Celeste.Android.csproj'
$out = Join-Path $root 'out'
New-Item -ItemType Directory -Force $out | Out-Null
foreach ($k in 'JAVA_HOME', 'ANDROID_HOME', 'ANDROID_NDK_HOME') { $v = [Environment]::GetEnvironmentVariable($k, 'User'); if ($v) { Set-Item "env:$k" $v } }

$version = ([xml](Get-Content $project)).Project.PropertyGroup.ApplicationDisplayVersion | Where-Object { $_ } | Select-Object -First 1

$signing = @()
$keyEnv = Join-Path $env:USERPROFILE '.celeste-android\keystore.env'
if (Test-Path $keyEnv) {
	$keys = @{}
	Get-Content $keyEnv | ForEach-Object { $name, $value = $_ -split '=', 2; $keys[$name] = $value }
	$signing = @(
		"-p:AndroidSigningKeyStore=$($keys.CELESTE_KEYSTORE)",
		"-p:AndroidSigningKeyAlias=$($keys.CELESTE_KEYSTORE_ALIAS)",
		"-p:AndroidSigningStorePass=$($keys.CELESTE_KEYSTORE_PASS)",
		"-p:AndroidSigningKeyPass=$($keys.CELESTE_KEYSTORE_PASS)"
	)
}
else {
	Write-Warning "No release keystore at ${keyEnv}: signing with the debug key."
}

& (Join-Path $PSScriptRoot 'generate-game-art.ps1') | Out-Null

if ($Personal) {
	$name = "CelesteAndroid-$version-personal.apk"
	$flags = @('-p:UseGameArt=true')
	if ($EmbedGame) { $flags += '-p:EmbedGame=true' }
}
else {
	$name = "CelesteAndroid-$version.apk"
	$flags = @('-p:UseGameArt=false')
}

$publish = Join-Path $root 'build\publish'
Remove-Item -Recurse -Force $publish -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force (Join-Path $root 'src\Celeste.Android\obj\Release'), (Join-Path $root 'src\Celeste.Android\bin\Release') -ErrorAction SilentlyContinue
dotnet publish $project -c Release -o $publish @flags @signing -nologo -v q | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }

$apk = Get-ChildItem $publish -Filter *-Signed.apk | Select-Object -First 1

if (-not $Personal) {
	Add-Type -AssemblyName System.IO.Compression.FileSystem
	$zip = [IO.Compression.ZipFile]::OpenRead($apk.FullName)
	try {
		$bad = $zip.Entries | Where-Object { $_.FullName -like 'assets/game/*' -or $_.FullName -match 'celeste_art|celeste_logo' }
		if ($bad) { throw "Public APK contains game files/key art/logo ($(@($bad).Count) entries); aborting." }
	}
	finally { $zip.Dispose() }
}
Copy-Item $apk.FullName (Join-Path $out $name) -Force
Get-Item (Join-Path $out $name) | Select-Object Name, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }
