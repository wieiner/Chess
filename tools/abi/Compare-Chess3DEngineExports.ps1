param(
    [string]$SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$HeaderPath = '',
    [string]$WindowsLibraryPath = '',
    [string]$LinuxLibraryPath = '',
    [switch]$ExpectedOnly
)

$ErrorActionPreference = 'Stop'

function Resolve-DefaultPath([string]$candidate) {
    if ([string]::IsNullOrWhiteSpace($candidate)) { return '' }
    if ([System.IO.Path]::IsPathRooted($candidate)) { return $candidate }
    return Join-Path $SourceRoot $candidate
}

function Get-ExpectedExports([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Header not found: $path"
    }
    $text = Get-Content -LiteralPath $path -Raw
    $matches = [regex]::Matches($text, 'CHESS3D_API\s+[^;\r\n]*?\s+(Chess3D_[A-Za-z0-9_]+)\s*\(')
    $names = New-Object System.Collections.Generic.HashSet[string]
    foreach ($match in $matches) {
        [void]$names.Add($match.Groups[1].Value)
    }
    $result = @()
    foreach ($name in $names) { $result += $name }
    return $result | Sort-Object
}

function Get-CommandPath([string[]]$names) {
    foreach ($name in $names) {
        $cmd = Get-Command $name -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
    }
    return ''
}

function Read-WindowsExports([string]$libraryPath) {
    if (-not (Test-Path -LiteralPath $libraryPath)) { return @() }

    $dumpbin = Get-CommandPath @('dumpbin.exe', 'dumpbin')
    if ($dumpbin) {
        $output = & $dumpbin /exports $libraryPath 2>$null
        return $output |
            ForEach-Object {
                if ($_ -match '\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(Chess3D_[A-Za-z0-9_]+)') { $Matches[1] }
            } |
            Sort-Object -Unique
    }

    $llvmObjdump = Get-CommandPath @('llvm-objdump.exe', 'llvm-objdump', 'C:\ll\local\bin\llvm-objdump.exe')
    if ($llvmObjdump) {
        $output = & $llvmObjdump -p $libraryPath 2>$null
        $exports = @($output |
            ForEach-Object {
                if ($_ -match '\b(Chess3D_[A-Za-z0-9_]+)\b') { $Matches[1] }
            } |
            Sort-Object -Unique)
        if ($exports.Count -gt 0) { return $exports }
    }

    $llvmNm = Get-CommandPath @('llvm-nm.exe', 'llvm-nm', 'C:\ll\local\bin\llvm-nm.exe')
    if ($llvmNm) {
        $output = & $llvmNm --defined-only $libraryPath 2>$null
        $exports = @($output |
            ForEach-Object {
                if ($_ -match '\b(Chess3D_[A-Za-z0-9_]+)\b') { $Matches[1] }
            } |
            Sort-Object -Unique)
        if ($exports.Count -gt 0) { return $exports }
    }

    Write-Warning 'No dumpbin/llvm-objdump export output found; Windows export comparison skipped.'
    return @()
}

function Read-LinuxExports([string]$libraryPath) {
    if (-not (Test-Path -LiteralPath $libraryPath)) { return @() }

    $nm = Get-CommandPath @('nm.exe', 'nm', 'llvm-nm.exe', 'llvm-nm', 'C:\ll\local\bin\llvm-nm.exe')
    if (-not $nm) {
        Write-Warning 'No nm/llvm-nm found; Linux export comparison skipped.'
        return @()
    }

    $output = & $nm -D --defined-only $libraryPath 2>$null
    if ($LASTEXITCODE -ne 0) {
        $output = & $nm --defined-only $libraryPath 2>$null
    }

    return $output |
        ForEach-Object {
            if ($_ -match '\b(Chess3D_[A-Za-z0-9_]+)\b') { $Matches[1] }
        } |
        Sort-Object -Unique
}

function Compare-Exports([string]$label, [string[]]$expected, [string[]]$actual, [bool]$actualAvailable) {
    if (-not $actualAvailable) {
        Write-Host "${label}: library not available; expected manifest only."
        return $true
    }

    if ($actual.Count -eq 0) {
        Write-Warning "${label}: no exports could be read."
        return $false
    }

    $actualSet = New-Object System.Collections.Generic.HashSet[string]
    foreach ($name in $actual) { [void]$actualSet.Add($name) }
    $missing = @($expected | Where-Object { -not $actualSet.Contains($_) })
    if ($missing.Count -gt 0) {
        Write-Error "$label missing required exports: $($missing -join ', ')"
        return $false
    }

    Write-Host "${label}: OK ($($actual.Count) Chess3D exports found; $($expected.Count) required exports present)."
    return $true
}

if ([string]::IsNullOrWhiteSpace($HeaderPath)) {
    $HeaderPath = Join-Path $SourceRoot 'src\Chess3DEngine\Chess3DEngine.h'
} else {
    $HeaderPath = Resolve-DefaultPath $HeaderPath
}
if ([string]::IsNullOrWhiteSpace($WindowsLibraryPath)) {
    $WindowsLibraryPath = Join-Path $SourceRoot 'bin\x64\Release\Chess3DEngine.dll'
} else {
    $WindowsLibraryPath = Resolve-DefaultPath $WindowsLibraryPath
}
if ([string]::IsNullOrWhiteSpace($LinuxLibraryPath)) {
    $LinuxLibraryPath = Join-Path $SourceRoot 'build-linux\libChess3DEngine.so'
} else {
    $LinuxLibraryPath = Resolve-DefaultPath $LinuxLibraryPath
}

$expected = @(Get-ExpectedExports $HeaderPath)
if ($expected.Count -eq 0) {
    throw 'No CHESS3D_API exports found in header.'
}

Write-Host "Expected Chess3D exports: $($expected.Count)"
if ($ExpectedOnly) {
    $expected | ForEach-Object { Write-Host "EXPECTED $_" }
    exit 0
}

$ok = $true
$windowsAvailable = Test-Path -LiteralPath $WindowsLibraryPath
$linuxAvailable = Test-Path -LiteralPath $LinuxLibraryPath

if (-not $windowsAvailable) { Write-Warning "Windows library not found: $WindowsLibraryPath" }
if (-not $linuxAvailable) { Write-Warning "Linux library not found: $LinuxLibraryPath" }

$windowsExports = @(Read-WindowsExports $WindowsLibraryPath)
$linuxExports = @(Read-LinuxExports $LinuxLibraryPath)

$ok = (Compare-Exports 'Windows Chess3DEngine.dll' $expected $windowsExports $windowsAvailable) -and $ok
$ok = (Compare-Exports 'Linux libChess3DEngine.so' $expected $linuxExports $linuxAvailable) -and $ok

if (-not $ok) { exit 1 }
exit 0
