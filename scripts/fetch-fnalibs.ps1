# Baixa as fnalibs (SDL3, FNA3D, FAudio) pré-compiladas para Windows x64 em natives/win-x64.
# Fonte: artifact "fnalibs" do CI de FNA-XNA/fnalibs-dailies, via nightly.link (sem login no GitHub).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dest = Join-Path $root 'natives\win-x64'
$tmp = Join-Path $env:TEMP 'fnalibs-fetch'

Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $tmp, $dest | Out-Null

curl.exe -sSL -o "$tmp\fnalibs.zip" https://nightly.link/FNA-XNA/fnalibs-dailies/workflows/ci/main/fnalibs.zip
if ($LASTEXITCODE -ne 0) { throw "download falhou ($LASTEXITCODE)" }

Expand-Archive "$tmp\fnalibs.zip" "$tmp\x"
Get-ChildItem "$tmp\x" -File | Where-Object { $_.Extension -in '.zip', '.tar', '.bz2', '.gz' } | ForEach-Object { tar -xf $_.FullName -C "$tmp\x" }
Copy-Item "$tmp\x\x64\*.dll" $dest -Force
Get-ChildItem $dest | Select-Object Name, Length
