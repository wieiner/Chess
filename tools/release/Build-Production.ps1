param(
    [ValidateSet("All", "Chess2D", "Chess3D", "Rubik", "Online", "Benchmark2D", "ChessOnline", "Chess2DBenchmark")]
    [string]$Product = "All",

    [switch]$CleanOnly,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir "..\.."))
$ProductionRoot = Join-Path $Root "ProductionOutput"
$Configuration = "Release"
$Platform = "x64"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "== $Message" -ForegroundColor Cyan
}

function Resolve-UnderRoot([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $rootFull = [System.IO.Path]::GetFullPath($Root)
    if (-not $rootFull.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $rootFull = $rootFull + [System.IO.Path]::DirectorySeparatorChar
    }
    if (-not ($full.Equals($Root, [System.StringComparison]::OrdinalIgnoreCase) -or
              $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase))) {
        throw "Refusing to touch path outside repository root: $full"
    }
    return $full
}

function Remove-SafeDirectory([string]$RelativePath) {
    $target = Resolve-UnderRoot (Join-Path $Root $RelativePath)
    if (Test-Path -LiteralPath $target) {
        Write-Host "Removing $RelativePath"
        Remove-Item -LiteralPath $target -Recurse -Force
    }
}

function Clean-Outputs {
    Write-Step "Cleaning previous build and portable outputs"

    $knownOutputDirs = @(
        "ProductionOutput",
        "dist",
        "bin",
        "obj",
        "src\ChessApp\bin",
        "src\ChessApp\obj",
        "src\Chess3DApp\bin",
        "src\Chess3DApp\obj",
        "src\ChessOnlineApp\bin",
        "src\ChessOnlineApp\obj",
        "src\RubikApp\bin",
        "src\RubikApp\obj",
        "src\Chess2DBenchmark\obj",
        "src\Chess2DBenchmark\x64",
        "src\ChessEngine\bin",
        "src\ChessEngine\obj",
        "src\ChessEngine\x64",
        "src\ChessGpuBackend\bin",
        "src\ChessGpuBackend\obj",
        "src\ChessGpuBackend\x64",
        "src\Chess3DEngine\bin",
        "src\Chess3DEngine\obj",
        "src\Chess3DEngine\x64",
        "src\ChessCudaBackend\bin",
        "src\ChessCudaBackend\obj",
        "src\ChessCudaBackend\x64",
        "src\ChessCudaBackend\ChessCudaBackend",
        "src\RubikEngine\bin",
        "src\RubikEngine\obj",
        "src\RubikEngine\x64"
    )

    foreach ($relative in $knownOutputDirs) {
        Remove-SafeDirectory $relative
    }
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

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    $cmd = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    throw "MSBuild.exe was not found. Install Visual Studio Build Tools with C++ workload."
}

function Build-Solution {
    Write-Step "Building solution Release x64"
    $msbuild = Resolve-MSBuild
    $solution = Join-Path $Root "Chess.sln"
    & $msbuild $solution "/restore" "/m" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:minimal"
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed with exit code $LASTEXITCODE."
    }
}

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Copy-FilteredDirectory([string]$Source, [string]$Destination) {
    $sourceFull = Resolve-UnderRoot $Source
    $destinationFull = Resolve-UnderRoot $Destination
    if (-not (Test-Path -LiteralPath $sourceFull)) {
        throw "Source output not found: $sourceFull"
    }

    Ensure-Directory $destinationFull
    $excludedExtensions = @(".pdb", ".ipdb", ".iobj", ".exp", ".lib", ".ilk")
    $excludedNames = @("*.tlog", "*.lastbuildstate", "*.recipe", "*.cache", "*.log")

    Get-ChildItem -LiteralPath $sourceFull -Recurse -File | ForEach-Object {
        $extension = $_.Extension.ToLowerInvariant()
        $skip = $false
        if ($excludedExtensions -contains $extension) {
            $skip = $true
        }
        foreach ($pattern in $excludedNames) {
            if ($_.Name -like $pattern) {
                $skip = $true
                break
            }
        }

        if (-not $skip) {
            $relative = $_.FullName.Substring($sourceFull.Length).TrimStart("\", "/")
            $target = Join-Path $destinationFull $relative
            Ensure-Directory (Split-Path -Parent $target)
            Copy-Item -LiteralPath $_.FullName -Destination $target -Force
        }
    }
}

function Copy-SelectedFiles([string]$Source, [string]$Destination, [string[]]$Files) {
    $sourceFull = Resolve-UnderRoot $Source
    $destinationFull = Resolve-UnderRoot $Destination
    Ensure-Directory $destinationFull
    foreach ($file in $Files) {
        $from = Join-Path $sourceFull $file
        if (-not (Test-Path -LiteralPath $from)) {
            throw "Required file not found: $from"
        }
        Copy-Item -LiteralPath $from -Destination (Join-Path $destinationFull $file) -Force
    }
}

function Copy-OptionalFiles([string]$Source, [string]$Destination, [string[]]$Files) {
    $sourceFull = Resolve-UnderRoot $Source
    $destinationFull = Resolve-UnderRoot $Destination
    Ensure-Directory $destinationFull
    foreach ($file in $Files) {
        $from = Join-Path $sourceFull $file
        if (Test-Path -LiteralPath $from) {
            Copy-Item -LiteralPath $from -Destination (Join-Path $destinationFull $file) -Force
        }
        else {
            Write-Host "Optional file not found, skipping: $file"
        }
    }
}

function Resolve-CudaRuntimeDll {
    $candidates = New-Object System.Collections.Generic.List[string]

    if ($env:CUDA_PATH) {
        $cudaPath = Join-Path $env:CUDA_PATH "bin\cudart64*.dll"
        foreach ($item in Get-ChildItem -Path $cudaPath -ErrorAction SilentlyContinue) {
            $candidates.Add($item.FullName)
        }
    }

    $defaultCudaRoot = "C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA"
    foreach ($item in Get-ChildItem -Path $defaultCudaRoot -Recurse -Filter "cudart64*.dll" -ErrorAction SilentlyContinue) {
        $candidates.Add($item.FullName)
    }

    foreach ($candidate in ($candidates | Sort-Object -Descending -Unique)) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

function Copy-CudaRuntimeIfNeeded([string]$Destination) {
    $destinationFull = Resolve-UnderRoot $Destination
    $cudaBackend = Join-Path $destinationFull "ChessCudaBackend.dll"
    if (-not (Test-Path -LiteralPath $cudaBackend)) {
        return
    }

    $runtime = Resolve-CudaRuntimeDll
    if ($runtime) {
        Copy-Item -LiteralPath $runtime -Destination (Join-Path $destinationFull (Split-Path -Leaf $runtime)) -Force
    }
    else {
        Write-Warning "ChessCudaBackend.dll was published to $destinationFull, but cudart64*.dll was not found. CUDA will require the NVIDIA CUDA runtime to be available on the target machine."
    }
}

function Write-Launcher([string]$Directory, [string]$FileName, [string]$ExeName, [switch]$Console) {
    $path = Join-Path $Directory $FileName
    if ($Console) {
        $content = @(
            "@echo off",
            "cd /d ""%~dp0""",
            """%~dp0$ExeName"" %*",
            "exit /b %ERRORLEVEL%"
        )
    }
    else {
        $content = @(
            "@echo off",
            "cd /d ""%~dp0""",
            "start """" ""%~dp0$ExeName"" %*",
            "exit /b 0"
        )
    }
    Set-Content -LiteralPath $path -Value $content -Encoding ASCII
}

function Product-List {
    if ($Product -eq "All") {
        return @("Chess2D", "Chess3D", "Rubik", "Online", "Benchmark2D")
    }
    if ($Product -eq "ChessOnline") {
        return @("Online")
    }
    if ($Product -eq "Chess2DBenchmark") {
        return @("Benchmark2D")
    }
    return @($Product)
}

function Publish-Products {
    Write-Step "Publishing portable products to ProductionOutput"
    Ensure-Directory $ProductionRoot

    foreach ($item in Product-List) {
        switch ($item) {
            "Chess2D" {
                $dst = Join-Path $ProductionRoot "Chess2D"
                Copy-FilteredDirectory (Join-Path $Root "src\ChessApp\bin\x64\Release\net8.0-windows") $dst
                Copy-CudaRuntimeIfNeeded $dst
                Write-Launcher $dst "run_chess_2d.bat" "ChessApp.exe"
            }
            "Chess3D" {
                $dst = Join-Path $ProductionRoot "Chess3D"
                Copy-FilteredDirectory (Join-Path $Root "src\Chess3DApp\bin\x64\Release\net8.0-windows") $dst
                Copy-CudaRuntimeIfNeeded $dst
                Write-Launcher $dst "run_chess_3d.bat" "Chess3DApp.exe"
            }
            "Rubik" {
                $dst = Join-Path $ProductionRoot "Rubik"
                Copy-FilteredDirectory (Join-Path $Root "src\RubikApp\bin\x64\Release\net8.0-windows") $dst
                Write-Launcher $dst "run_rubik.bat" "RubikApp.exe"
            }
            "Online" {
                $dst = Join-Path $ProductionRoot "ChessOnlineIntegrations"
                Copy-FilteredDirectory (Join-Path $Root "src\ChessOnlineApp\bin\x64\Release\net8.0-windows") $dst
                Write-Launcher $dst "run_online.bat" "ChessOnlineApp.exe"
            }
            "Benchmark2D" {
                $dst = Join-Path $ProductionRoot "Chess2DBenchmark"
                Copy-SelectedFiles (Join-Path $Root "bin\x64\Release") $dst @(
                    "Chess2DBenchmark.exe",
                    "ChessEngine.dll",
                    "ChessGpuBackend.dll"
                )
                Copy-OptionalFiles (Join-Path $Root "bin\x64\Release") $dst @(
                    "ChessCudaBackend.dll"
                )
                Copy-CudaRuntimeIfNeeded $dst
                Write-Launcher $dst "run_benchmark_2d.bat" "Chess2DBenchmark.exe" -Console
            }
        }
    }

    Write-ProductionReadme
}

function Write-ProductionReadme {
    $readme = @"
Chess production portable output
================================

This folder is generated by package_all.bat.

Products:
- Chess2D\ChessApp.exe
- Chess3D\Chess3DApp.exe
- ChessOnlineIntegrations\ChessOnlineApp.exe
- Rubik\RubikApp.exe
- Chess2DBenchmark\Chess2DBenchmark.exe

The product folders intentionally exclude build intermediates, PDB files, import libraries,
old dist folders, and benchmark result CSV files. Assets and runtimeconfig/deps files are
kept because the applications need them at runtime.

If the CUDA backend is present and the local CUDA runtime DLL is available during packaging,
cudart64*.dll is copied next to the executable. Without it, the applications can still fall
back to CPU/Direct3D paths, but CUDA execution requires the NVIDIA driver/runtime stack.
"@
    Set-Content -LiteralPath (Join-Path $ProductionRoot "README.txt") -Value $readme -Encoding ASCII
}

Push-Location $Root
try {
    Clean-Outputs
    if ($CleanOnly) {
        Write-Step "Clean complete"
        return
    }

    if (-not $SkipBuild) {
        Build-Solution
    }

    Publish-Products
    Write-Step "Done"
    Write-Host "Portable output: $ProductionRoot" -ForegroundColor Green
}
finally {
    Pop-Location
}
