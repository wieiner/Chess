param(
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

    foreach ($root in $roots) {
        $candidates.Add((Join-Path $root "MSBuild\Current\Bin\amd64\MSBuild.exe"))
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

function Assert-File([string]$RelativePath) {
    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Expected file was not found: $RelativePath"
    }
    Write-Host "OK $RelativePath"
}

Push-Location $Root
try {
    Write-Step "Environment"
    Write-Host "Root: $Root"
    Write-Host "PowerShell: $($PSVersionTable.PSVersion)"
    Write-Host "MSBuild: $(Resolve-MSBuild)"
    Write-Host "nvcc:"
    cmd /c where nvcc
    if ($LASTEXITCODE -ne 0) { Write-Host "nvcc not found in PATH; CUDA backend remains optional." }
    Write-Host "cl:"
    cmd /c where cl
    if ($LASTEXITCODE -ne 0) { Write-Host "cl not found in shell PATH; MSBuild can still resolve VC tools." }
    dotnet --info

    Write-Step "Git status"
    git status --short
    if ($LASTEXITCODE -ne 0) { throw "git status failed." }
    git branch --show-current
    git remote -v

    Write-Step "Ignored resource archive"
    $ignoredProbe = "rude-resource/.verify-ignore-probe"
    git check-ignore -q -- $ignoredProbe
    if ($LASTEXITCODE -ne 0) {
        throw "rude-resource/ contents must be ignored by Git."
    }
    git check-ignore -v -- $ignoredProbe

    Write-Step "Build Release x64"
    $msbuild = Resolve-MSBuild
    Invoke-Checked { & $msbuild ".\Chess.sln" "/restore" "/m" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:minimal" } "MSBuild failed."

    Write-Step "Development executable checks"
    Assert-File "src\ChessApp\bin\x64\Release\net8.0-windows\ChessApp.exe"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Chess3DApp.exe"
    Assert-File "src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe"
    Assert-File "src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe"
    Assert-File "bin\x64\Release\Chess2DBenchmark.exe"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Profiles\asgard_convergence_3d_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Profiles\rubik_convergence_3d_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Profiles\hodge_projection_duel_3d_v0_1.json"

    Write-Step "Production packaging"
    Invoke-Checked { powershell -NoProfile -ExecutionPolicy Bypass -File ".\tools\release\Build-Production.ps1" -Product All } "Production packaging failed."

    Write-Step "Portable executable checks"
    Assert-File "ProductionOutput\Chess2D\ChessApp.exe"
    Assert-File "ProductionOutput\Chess3D\Chess3DApp.exe"
    Assert-File "ProductionOutput\Rubik\RubikApp.exe"
    Assert-File "ProductionOutput\ChessOnlineIntegrations\ChessOnlineApp.exe"
    Assert-File "ProductionOutput\Chess2DBenchmark\Chess2DBenchmark.exe"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Profiles\asgard_convergence_3d_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Profiles\rubik_convergence_3d_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Profiles\hodge_projection_duel_3d_v0_1.json"

    $excludedExtensions = @(".pdb", ".ipdb", ".iobj", ".lib", ".exp", ".ilk")
    $excludedNames = @("*.tlog", "*.lastbuildstate", "*.recipe", "*.cache", "*.log")
    $badPortableFiles = Get-ChildItem -LiteralPath (Join-Path $Root "ProductionOutput") -Recurse -File | Where-Object {
        $extension = $_.Extension.ToLowerInvariant()
        if ($excludedExtensions -contains $extension) {
            return $true
        }
        foreach ($pattern in $excludedNames) {
            if ($_.Name -like $pattern) {
                return $true
            }
        }
        return $false
    }
    if ($badPortableFiles) {
        $badPortableFiles | Select-Object -ExpandProperty FullName
        throw "ProductionOutput contains build intermediates or debug leftovers."
    }

    Write-Step "Contract tests"
    $testScript = Join-Path $Root "tests\run-tests.ps1"
    if (-not (Test-Path -LiteralPath $testScript -PathType Leaf)) {
        throw "Contract test runner is missing: $testScript"
    }
    if ($SkipBenchmark) {
        Invoke-Checked { powershell -NoProfile -ExecutionPolicy Bypass -File ".\tests\run-tests.ps1" -SkipSolutionBuild -SkipBenchmark } "Contract tests failed."
    }
    else {
        Invoke-Checked { powershell -NoProfile -ExecutionPolicy Bypass -File ".\tests\run-tests.ps1" -SkipSolutionBuild } "Contract tests failed."
    }

    Write-Step "Verify complete"
    exit 0
}
catch {
    Write-Error $_
    exit 1
}
finally {
    Pop-Location
}
