param(
	[string]$Abi = 'arm64-v8a',
	[int]$ApiLevel = 26,
	[switch]$Clean
)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$ndk = $env:ANDROID_NDK_HOME
if (-not $ndk -or -not (Test-Path "$ndk\build\cmake\android.toolchain.cmake")) {
	$ndk = [Environment]::GetEnvironmentVariable('ANDROID_NDK_HOME', 'User')
}
if (-not (Test-Path "$ndk\build\cmake\android.toolchain.cmake")) { throw "NDK não encontrado (ANDROID_NDK_HOME)." }

$abiDir = @{ 'arm64-v8a' = 'android-arm64'; 'x86_64' = 'android-x64' }[$Abi]
$build = Join-Path $root "build\$abiDir"
$out = Join-Path $root "natives\$abiDir"
if ($Clean) { Remove-Item -Recurse -Force $build -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force $build, $out | Out-Null

$common = @(
	'-G', 'Ninja',
	"-DCMAKE_TOOLCHAIN_FILE=$ndk/build/cmake/android.toolchain.cmake",
	"-DANDROID_ABI=$Abi",
	"-DANDROID_PLATFORM=android-$ApiLevel",
	'-DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON',
	'-DCMAKE_BUILD_TYPE=Release'
)

function Invoke-CMakeBuild([string]$name, [string]$src, [string[]]$extra) {
	Write-Host "=== $name ===" -ForegroundColor Cyan
	$dir = Join-Path $build $name
	cmake -S $src -B $dir @common @extra | Out-Host
	if ($LASTEXITCODE -ne 0) { throw "configure $name falhou" }
	cmake --build $dir --parallel | Out-Host
	if ($LASTEXITCODE -ne 0) { throw "build $name falhou" }
	return $dir
}

$sdlDir = Invoke-CMakeBuild 'SDL3' "$root\external\SDL" @(
	'-DSDL_SHARED=ON', '-DSDL_STATIC=OFF', '-DSDL_TEST_LIBRARY=OFF', '-DSDL_TESTS=OFF', '-DSDL_EXAMPLES=OFF'
)
$sdlSo = Join-Path $sdlDir 'libSDL3.so'
$sdl3 = @(
	"-DSDL3_INCLUDE_DIRS=$root/external/SDL/include",
	"-DSDL3_LIBRARIES=$sdlSo"
)

$fna3dDir = Invoke-CMakeBuild 'FNA3D' "$root\external\FNA\lib\FNA3D" (@('-DBUILD_SDL3=ON') + $sdl3)
$faudioDir = Invoke-CMakeBuild 'FAudio' "$root\external\FNA\lib\FAudio" (@('-DBUILD_SDL3=ON') + $sdl3)

$strip = Join-Path $ndk 'toolchains\llvm\prebuilt\windows-x86_64\bin\llvm-strip.exe'
foreach ($so in @($sdlSo, "$fna3dDir\libFNA3D.so", "$faudioDir\libFAudio.so")) {
	$dest = Join-Path $out (Split-Path $so -Leaf)
	Copy-Item $so $dest -Force
	& $strip --strip-unneeded $dest
}
Get-ChildItem $out -Filter *.so | Select-Object Name, Length
