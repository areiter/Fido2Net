param(
    [string]$CMakePath = "C:\Program Files\CMake\bin\cmake.exe",
    [string]$GitPath = "C:\Program Files\Git\bin\git.exe"
)

$Git = $(Get-Command git -ErrorAction Ignore) | Select-Object -ExpandProperty Source
if([string]::IsNullOrEmpty($Git)) {
    $Git = $GitPath
}

if(-Not (Test-Path $GitPath)) {
    throw "Unable to locate git at $Git"
}

$CMake = $(Get-Command cmake -ErrorAction Ignore) | Select-Object -ExpandProperty Source
if([string]::IsNullOrEmpty($CMake)) {
    $CMake = $CMakePath
}

if(-Not (Test-Path $CMakePath)) {
    throw "Unable to locate cmake at $CMake"
}

Push-Location $PSScriptRoot
try {
    if(-Not (Test-Path libfido2)) {
        & $Git clone https://github.com/yubico/libfido2 --branch 1.12.0
    }

    libfido2\windows\build.ps1
    New-Item -Type Directory -ErrorAction Ignore .\Fido2Net\Native
    Copy-Item .\libfido2\build\x64\dynamic\src\Release\fido2.dll Fido2Net\Native\
    Copy-Item .\libfido2\build\x64\dynamic\libressl-3.5.3\crypto\Release\crypto-49.dll .\Fido2Net\Native\
    Copy-Item .\libfido2\build\x64\dynamic\zlib-1.2.12\Release\zlib1.dll .\Fido2Net\Native
    Copy-Item .\libfido2\build\x64\dynamic\libcbor-0.9.0\src\Release\cbor.dll .\Fido2Net\Native
} finally {
    Pop-Location
}