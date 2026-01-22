# Reading a Book Dual Release Build Script
param([string]$Version = "")

$ErrorActionPreference = "Stop"

# アプリ情報を読み込む
$appInfo = @{}
if (Test-Path "app-info.txt") {
    Get-Content "app-info.txt" | ForEach-Object {
        if ($_ -match '^([^#][^=]+)=(.+)$') {
            $appInfo[$matches[1].Trim()] = $matches[2].Trim()
        }
    }
}

$AppName = $appInfo["APP_NAME"]
if ([string]::IsNullOrEmpty($Version)) {
    $Version = $appInfo["APP_VERSION"]
}

Write-Host "$AppName v$Version Dual Release Build" -ForegroundColor Cyan
Write-Host ""

$ProjectFile = "ScreenPresenterAssist.csproj"
$DistDir = "dist"
$TempFrameworkDir = "$DistDir\temp_framework"
$TempStandaloneDir = "$DistDir\temp_standalone"
$FrameworkZip = "$DistDir\$AppName-v$Version-framework-dependent-release.zip"
$StandaloneZip = "$DistDir\$AppName-v$Version-standalone-release.zip"
$BuildStartTime = Get-Date

# Cleanup
Write-Host "1. Cleanup..." -ForegroundColor Yellow
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
if (Test-Path "bin\Release") { Remove-Item "bin\Release" -Recurse -Force }
if (Test-Path "obj\Release") { Remove-Item "obj\Release" -Recurse -Force }
Write-Host "   Done" -ForegroundColor Green
Write-Host ""

# Framework-dependent build
Write-Host "2. Framework-Dependent Build..." -ForegroundColor Yellow
$fwSuccess = $false
try {
    dotnet publish $ProjectFile -c Release -r win-x64 --self-contained false -o $TempFrameworkDir
    if ($LASTEXITCODE -eq 0) {
        # exeファイル名はそのまま
        if (Test-Path "README.md") { Copy-Item "README.md" "$TempFrameworkDir\" -Force }
        Compress-Archive -Path "$TempFrameworkDir\*" -DestinationPath $FrameworkZip -Force
        Write-Host "   Done" -ForegroundColor Green
        $fwSuccess = $true
    }
}
catch {
    Write-Host "   Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Self-contained build
Write-Host ""
Write-Host "3. Self-Contained Build..." -ForegroundColor Yellow
$saSuccess = $false
try {
    dotnet publish $ProjectFile -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $TempStandaloneDir
    if ($LASTEXITCODE -eq 0) {
        # exeファイル名はそのまま
        if (Test-Path "README.md") { Copy-Item "README.md" "$TempStandaloneDir\" -Force }
        Compress-Archive -Path "$TempStandaloneDir\*" -DestinationPath $StandaloneZip -Force
        Write-Host "   Done" -ForegroundColor Green
        $saSuccess = $true
    }
}
catch {
    Write-Host "   Failed: $($_.Exception.Message)" -ForegroundColor Red
}

if (-not $fwSuccess -and -not $saSuccess) {
    Write-Error "Both builds failed"
    exit 1
}

# Cleanup temp
Write-Host ""
Write-Host "4. Cleanup temp files..." -ForegroundColor Yellow
if (Test-Path $TempFrameworkDir) { Remove-Item $TempFrameworkDir -Recurse -Force }
if (Test-Path $TempStandaloneDir) { Remove-Item $TempStandaloneDir -Recurse -Force }
Write-Host "   Done" -ForegroundColor Green
Write-Host ""

# Summary
$BuildTime = [math]::Round(((Get-Date) - $BuildStartTime).TotalSeconds, 1)
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host ""

if ($fwSuccess -and (Test-Path $FrameworkZip)) {
    $info = Get-Item $FrameworkZip
    $hash = Get-FileHash $FrameworkZip -Algorithm SHA256
    Write-Host "Framework-Dependent:" -ForegroundColor Cyan
    Write-Host "  File: $($info.Name)" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($info.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "  SHA256: $($hash.Hash)" -ForegroundColor Gray
    Write-Host "  Requires .NET 9.0 Runtime" -ForegroundColor Yellow
    Write-Host ""
}

if ($saSuccess -and (Test-Path $StandaloneZip)) {
    $info = Get-Item $StandaloneZip
    $hash = Get-FileHash $StandaloneZip -Algorithm SHA256
    Write-Host "Self-Contained:" -ForegroundColor Cyan
    Write-Host "  File: $($info.Name)" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($info.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "  SHA256: $($hash.Hash)" -ForegroundColor Gray
    Write-Host "  No runtime required" -ForegroundColor Green
    Write-Host ""
}

Write-Host "Build time: $BuildTime seconds" -ForegroundColor White
Write-Host "Output: $DistDir" -ForegroundColor White
Write-Host ""
