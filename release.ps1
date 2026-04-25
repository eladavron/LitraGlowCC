# Read src\package\metadata\LoupedeckPackage.yaml and extract the version number
$yaml = Get-Content -Raw -Path "src\package\metadata\LoupedeckPackage.yaml"
$version = ($yaml -split "`n" | Where-Object { $_ -match "^version:" }) -replace "version:\s*", ""

Write-Output "Packaging version $version"

logiplugintool pack ".\bin\Release\" "LitraGlowCC_${version}.lplug4"