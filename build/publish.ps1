# Genera un unico ejecutable autocontenido para Windows x64.
# Uso: pwsh ./build/publish.ps1   (o powershell -File build\publish.ps1)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out  = Join-Path $root "publish"

dotnet test  (Join-Path $root "Cotizador3D.sln") -c Release
dotnet publish (Join-Path $root "src/Cotizador3D.App/Cotizador3D.App.csproj") -c Release -o $out

Write-Host "Ejecutable generado en $out"
Get-ChildItem $out -Filter *.exe | ForEach-Object { "{0}  {1:N1} MB" -f $_.Name, ($_.Length / 1MB) }
