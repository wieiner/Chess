param(
    [switch]$SkipSolutionBuild,
    [switch]$SkipBenchmark
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir ".."))
$Configuration = "Release"
$Platform = "x64"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "== $Message" -ForegroundColor Cyan
}

function Resolve-MSBuild {
    $candidates = New-Object System.Collections.Generic.List[string]
    $vswhereCandidates = @(
        "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe",
        "C:\Program Files\Microsoft Visual Studio\Installer\vswhere.exe"
    )

    foreach ($vswhere in $vswhereCandidates) {
        if (Test-Path -LiteralPath $vswhere) {
            $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\amd64\MSBuild.exe" 2>$null
            foreach ($item in $found) {
                if ($item) { $candidates.Add($item) }
            }
        }
    }

    $roots = @(
        "C:\Program Files\Microsoft Visual Studio\18\Enterprise",
        "C:\Program Files\Microsoft Visual Studio\18\Professional",
        "C:\Program Files\Microsoft Visual Studio\18\Community",
        "C:\Program Files\Microsoft Visual Studio\18\BuildTools",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional",
        "C:\Program Files\Microsoft Visual Studio\2022\Community",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools"
    )

    foreach ($rootPath in $roots) {
        $candidates.Add((Join-Path $rootPath "MSBuild\Current\Bin\amd64\MSBuild.exe"))
    }

    $cmd = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        $candidates.Add($cmd.Source)
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "MSBuild.exe was not found. Install Visual Studio Build Tools with C++ workload."
}

function Invoke-Checked([scriptblock]$Command, [string]$FailureMessage) {
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage Exit code: $LASTEXITCODE"
    }
}

function Build-TestProject([string]$ProjectPath) {
    if (-not (Test-Path -LiteralPath $ProjectPath)) {
        throw "Contract test project is missing: $ProjectPath"
    }
    $msbuild = Resolve-MSBuild
    Invoke-Checked { & $msbuild $ProjectPath "/m" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:minimal" } "Contract test build failed: $ProjectPath"
}

function Invoke-TestExecutable([string]$Name, [string]$ExePath, [string[]]$Arguments) {
    if (-not (Test-Path -LiteralPath $ExePath)) {
        throw "Contract test executable is missing: $ExePath"
    }

    Write-Step $Name
    & $ExePath @Arguments | ForEach-Object { Write-Host $_ }
    $exitCode = $LASTEXITCODE
    return [PSCustomObject]@{
        Name = $Name
        ExitCode = $exitCode
        Result = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
    }
}

Push-Location $Root
try {
    $nativeBin = Join-Path $Root "bin\$Platform\$Configuration"
    $oldPath = $env:PATH
    $env:PATH = "$nativeBin;$oldPath"

    if (-not $SkipSolutionBuild) {
        Write-Step "Build solution Release x64"
        $msbuild = Resolve-MSBuild
        Invoke-Checked { & $msbuild ".\Chess.sln" "/restore" "/m" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:minimal" } "Solution build failed."
    }

    $tests = @(
        @{ Name = "ChessEngineContractTests"; Project = "tests\ChessEngineContractTests\ChessEngineContractTests.vcxproj"; Exe = "tests\bin\x64\Release\ChessEngineContractTests\ChessEngineContractTests.exe" },
        @{ Name = "Chess3DEngineContractTests"; Project = "tests\Chess3DEngineContractTests\Chess3DEngineContractTests.vcxproj"; Exe = "tests\bin\x64\Release\Chess3DEngineContractTests\Chess3DEngineContractTests.exe" },
        @{ Name = "RubikEngineContractTests"; Project = "tests\RubikEngineContractTests\RubikEngineContractTests.vcxproj"; Exe = "tests\bin\x64\Release\RubikEngineContractTests\RubikEngineContractTests.exe" },
        @{ Name = "GpuBackendContractTests"; Project = "tests\GpuBackendContractTests\GpuBackendContractTests.vcxproj"; Exe = "tests\bin\x64\Release\GpuBackendContractTests\GpuBackendContractTests.exe" }
    )

    Write-Step "Build contract tests"
    foreach ($item in $tests) {
        Build-TestProject (Join-Path $Root $item.Project)
    }

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($item in $tests) {
        $results.Add((Invoke-TestExecutable $item.Name (Join-Path $Root $item.Exe) @()))
    }

    if (-not $SkipBenchmark) {
        $benchmark = Join-Path $nativeBin "Chess2DBenchmark.exe"
        if (Test-Path -LiteralPath $benchmark) {
            $results.Add((Invoke-TestExecutable "Chess2DBenchmark --quick" $benchmark @("--quick")))
        }
        else {
            Write-Host "SKIP Chess2DBenchmark --quick: executable not found at $benchmark"
        }
    }

    Write-Step "Contract test summary"
    $results | Format-Table -AutoSize

    $failed = @($results | Where-Object { $_.ExitCode -ne 0 })
    if ($failed.Count -gt 0) {
        exit 1
    }

    exit 0
}
catch {
    Write-Error $_
    exit 1
}
finally {
    $env:PATH = $oldPath
    Pop-Location
}
