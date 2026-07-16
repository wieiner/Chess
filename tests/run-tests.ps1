param(
    [switch]$SkipSolutionBuild,
    [switch]$SkipTestBuild,
    [switch]$SkipBenchmark,

    [ValidateSet("All", "Native", "Managed", "Online", "Chess3D", "Gpu", "Rubik", "Chess2D")]
    [string]$Suite = "All",

    [string]$Only = "",

    [int]$MSBuildMaxCpuCount = 0,
    [int]$TestTimeoutSeconds = 120,
    [int]$OnlineTestTimeoutSeconds = 180,
    [int]$GlobalTimeoutSeconds = 900,

    [switch]$BuildOnly,
    [switch]$List,
    [switch]$NoParallelBuild,
    [switch]$CleanStaleBuildProcesses
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir ".."))
$Configuration = "Release"
$Platform = "x64"
$script:GlobalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$script:TestLogRoot = Join-Path $Root ".tmp\test-logs"

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
                if (-not [string]::IsNullOrWhiteSpace($item)) { $candidates.Add($item) }
            }
        }
    }
    foreach ($candidate in @(
        "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
    )) { $candidates.Add($candidate) }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
    throw "MSBuild.exe was not found."
}

function Resolve-MSBuildMaxCpuCountValue {
    if ($NoParallelBuild) { return 1 }
    if ($MSBuildMaxCpuCount -gt 0) { return $MSBuildMaxCpuCount }
    $value = $env:CHESS_TEST_MSBUILD_MAX_CPU_COUNT
    if ([string]::IsNullOrWhiteSpace($value)) { $value = $env:CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT }
    if ([string]::IsNullOrWhiteSpace($value)) { return 4 }
    if ($value -match '^[1-9][0-9]*$') { return [int]$value }
    throw "Invalid MSBuild CPU count: $value"
}

function Assert-GlobalTimeout([string]$StepName) {
    if ($GlobalTimeoutSeconds -le 0) { return }
    if ($script:GlobalStopwatch.Elapsed.TotalSeconds -gt $GlobalTimeoutSeconds) {
        throw "Global test runner timeout exceeded before ${StepName}: limit ${GlobalTimeoutSeconds}s, elapsed $([int]$script:GlobalStopwatch.Elapsed.TotalSeconds)s."
    }
}

function Stop-StaleBuildProcessesIfRequested {
    if (-not $CleanStaleBuildProcesses) { return }
    Write-Step "Clean stale build processes"
    $escapedRoot = [Regex]::Escape($Root)
    $processes = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
        $name = $_.Name
        $commandLine = [string]$_.CommandLine
        if ($name -in @("MSBuild.exe", "VBCSCompiler.exe")) { return $true }
        if ($name -eq "dotnet.exe" -and $commandLine -match $escapedRoot) { return $true }
        return $false
    })
    if ($processes.Count -eq 0) { Write-Host "No stale build processes found."; return }
    $processes | Select-Object ProcessId, Name, CreationDate, CommandLine | Format-Table -AutoSize
    foreach ($process in $processes) {
        Stop-Process -Id $process.ProcessId -Force
    }
}

function New-TestRegistry {
    $nativeBase = "tests\bin\x64\Release"
    return @(
        [PSCustomObject]@{ Name = "ChessEngineContractTests"; Suites = @("All", "Native", "Chess2D"); Project = "tests\ChessEngineContractTests\ChessEngineContractTests.vcxproj"; Exe = "$nativeBase\ChessEngineContractTests\ChessEngineContractTests.exe"; Arguments = @(); IsOnline = $false; IsBenchmark = $false },
        [PSCustomObject]@{ Name = "Chess3DEngineContractTests"; Suites = @("All", "Native", "Chess3D"); Project = "tests\Chess3DEngineContractTests\Chess3DEngineContractTests.vcxproj"; Exe = "$nativeBase\Chess3DEngineContractTests\Chess3DEngineContractTests.exe"; Arguments = @(); IsOnline = $false; IsBenchmark = $false },
        [PSCustomObject]@{ Name = "RubikEngineContractTests"; Suites = @("All", "Native", "Rubik"); Project = "tests\RubikEngineContractTests\RubikEngineContractTests.vcxproj"; Exe = "$nativeBase\RubikEngineContractTests\RubikEngineContractTests.exe"; Arguments = @(); IsOnline = $false; IsBenchmark = $false },
        [PSCustomObject]@{ Name = "RubikVisualContractTests"; Suites = @("All", "Managed", "Rubik"); Project = "tests\RubikVisualContractTests\RubikVisualContractTests.csproj"; Exe = "tests\RubikVisualContractTests\bin\x64\Release\net8.0\RubikVisualContractTests.exe"; Arguments = @(); IsOnline = $false; IsBenchmark = $false },
        [PSCustomObject]@{ Name = "GpuBackendContractTests"; Suites = @("All", "Native", "Gpu"); Project = "tests\GpuBackendContractTests\GpuBackendContractTests.vcxproj"; Exe = "$nativeBase\GpuBackendContractTests\GpuBackendContractTests.exe"; Arguments = @(); IsOnline = $false; IsBenchmark = $false },
        [PSCustomObject]@{ Name = "ChessOnlineContractTests"; Suites = @("All", "Managed", "Online"); Project = "tests\ChessOnlineContractTests\ChessOnlineContractTests.csproj"; Exe = "tests\ChessOnlineContractTests\bin\x64\Release\net8.0-windows\ChessOnlineContractTests.exe"; Arguments = @(); IsOnline = $true; IsBenchmark = $false },
        [PSCustomObject]@{ Name = "ChessOnlineSignalRContractTests"; Suites = @("All", "Managed", "Online"); Project = "tests\ChessOnlineSignalRContractTests\ChessOnlineSignalRContractTests.csproj"; Exe = "tests\ChessOnlineSignalRContractTests\bin\x64\Release\net8.0-windows\ChessOnlineSignalRContractTests.exe"; Arguments = @(); IsOnline = $true; IsBenchmark = $false },
        [PSCustomObject]@{ Name = "Chess2DBenchmarkQuick"; Suites = @("All", "Chess2D"); Project = ""; Exe = "bin\x64\Release\Chess2DBenchmark.exe"; Arguments = @("--quick"); IsOnline = $false; IsBenchmark = $true }
    )
}

function Select-Tests([object[]]$Registry) {
    $selected = @($Registry | Where-Object { $_.Suites -contains $Suite })
    if ($SkipBenchmark) { $selected = @($selected | Where-Object { -not $_.IsBenchmark }) }
    if (-not [string]::IsNullOrWhiteSpace($Only)) { $selected = @($selected | Where-Object { $_.Name -match $Only }) }
    return $selected
}

function Show-TestList([object[]]$Tests) {
    foreach ($test in $Tests) {
        $timeout = if ($test.IsOnline) { $OnlineTestTimeoutSeconds } else { $TestTimeoutSeconds }
        Write-Host ("{0} | Suites={1} | Timeout={2}s | Project={3} | Exe={4}" -f $test.Name, ($test.Suites -join ","), $timeout, $test.Project, $test.Exe)
    }
}

function Invoke-BuildProject([object]$Item, [string]$MSBuildPath, [int]$MaxCpuCount) {
    if ([string]::IsNullOrWhiteSpace($Item.Project)) {
        return [PSCustomObject]@{ Name = $Item.Name; BuildResult = "SKIP"; BuildLog = "" }
    }
    $projectPath = Join-Path $Root $Item.Project
    if (-not (Test-Path -LiteralPath $projectPath)) {
        return [PSCustomObject]@{ Name = $Item.Name; BuildResult = "FAIL"; BuildLog = "Missing project: $projectPath" }
    }
    Write-Step "Build $($Item.Name)"
    $buildLog = Join-Path $script:TestLogRoot "$($Item.Name).build.log"
    & $MSBuildPath $projectPath "/m:$MaxCpuCount" "/nr:false" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:minimal" *> $buildLog
    $exitCode = $LASTEXITCODE
    $buildResult = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
    if ($exitCode -ne 0 -and $MaxCpuCount -gt 1) {
        $retryLog = Join-Path $script:TestLogRoot "$($Item.Name).build.retry-m1.log"
        Write-Warning "MSBuild failed for $($Item.Name) under /m:$MaxCpuCount; retrying /m:1."
        & $MSBuildPath $projectPath "/m:1" "/nr:false" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:minimal" *> $retryLog
        $exitCode = $LASTEXITCODE
        $buildLog = $retryLog
        $buildResult = if ($exitCode -eq 0) { "PASS_RETRY" } else { "FAIL" }
    }
    Get-Content -LiteralPath $buildLog -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
    return [PSCustomObject]@{ Name = $Item.Name; BuildResult = $buildResult; BuildLog = $buildLog }
}

function Get-LogTail([string]$Path, [int]$Count = 80) {
    if (-not (Test-Path -LiteralPath $Path)) { return "" }
    return ((Get-Content -LiteralPath $Path -Tail $Count -ErrorAction SilentlyContinue) -join [Environment]::NewLine)
}

function Resolve-TestProcessWatchdog {
    $watchdog = Join-Path $Root "tools\TestProcessWatchdog\bin\Release\net8.0\TestProcessWatchdog.exe"
    if (Test-Path -LiteralPath $watchdog) { return $watchdog }
    $project = Join-Path $Root "tools\TestProcessWatchdog\TestProcessWatchdog.csproj"
    if (-not (Test-Path -LiteralPath $project)) { throw "C# process watchdog project is missing: $project" }
    Write-Step "Build C# process watchdog"
    $output = & dotnet build $project -c Release 2>&1
    foreach ($line in @($output)) { Write-Host $line }
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $watchdog)) { throw "C# process watchdog build failed." }
    return $watchdog
}

function Invoke-TestExecutable([object]$Item, [string]$LogRoot) {
    $exePath = Join-Path $Root $Item.Exe
    $timeout = if ($Item.IsOnline) { $OnlineTestTimeoutSeconds } else { $TestTimeoutSeconds }
    if (-not (Test-Path -LiteralPath $exePath)) {
        return [PSCustomObject]@{ Name = $Item.Name; RunResult = "FAIL"; ExitCode = -1; TimeoutSeconds = $timeout; StdoutLog = ""; StderrLog = "Missing executable: $exePath" }
    }

    $watchdog = Resolve-TestProcessWatchdog
    Write-Step "Run $($Item.Name)"
    $stdout = Join-Path $LogRoot "$($Item.Name).stdout.log"
    $stderr = Join-Path $LogRoot "$($Item.Name).stderr.log"
    Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue
    $watchdogArgs = @("--file", $exePath, "--workdir", $Root, "--timeout", ([string]$timeout), "--stdout", $stdout, "--stderr", $stderr)
    $testArgs = @($Item.Arguments)
    if ($testArgs.Count -gt 0) { $watchdogArgs += @("--args", ($testArgs -join " ")) }
    $watchdogOutput = & $watchdog @watchdogArgs 2>&1
    $exitCode = $LASTEXITCODE
    foreach ($line in @($watchdogOutput)) { Write-Host $line }
    $tail = Get-LogTail $stdout 120
    if ($tail) { Write-Host $tail }
    $errTail = Get-LogTail $stderr 120
    if ($errTail) { Write-Host $errTail -ForegroundColor Yellow }
    $runResult = if ($exitCode -eq 0) { "PASS" } elseif ($exitCode -eq 124) { "TIMEOUT" } else { "FAIL" }
    if ($runResult -eq "TIMEOUT") { Write-Warning "TIMEOUT $($Item.Name) after ${timeout}s. Logs: $stdout ; $stderr" }
    return [PSCustomObject]@{ Name = $Item.Name; RunResult = $runResult; ExitCode = $exitCode; TimeoutSeconds = $timeout; StdoutLog = $stdout; StderrLog = $stderr }
}

Push-Location $Root
try {
    Stop-StaleBuildProcessesIfRequested
    if (-not (Test-Path -LiteralPath $script:TestLogRoot)) { New-Item -ItemType Directory -Path $script:TestLogRoot | Out-Null }
    $oldPath = $env:PATH
    $env:PATH = "$(Join-Path $Root "bin\$Platform\$Configuration");$oldPath"
    $maxCpu = Resolve-MSBuildMaxCpuCountValue
    Write-Step "Test runner configuration"
    Write-Host "Suite: $Suite"
    Write-Host "Only: $Only"
    Write-Host "MSBuild: /m:$maxCpu /nr:false"
    Write-Host "Test timeout: ${TestTimeoutSeconds}s"
    Write-Host "Online test timeout: ${OnlineTestTimeoutSeconds}s"
    Write-Host "Global timeout: ${GlobalTimeoutSeconds}s"

    $registry = New-TestRegistry
    $tests = @(Select-Tests $registry)
    if ($tests.Count -eq 0) { throw "No tests selected for Suite=$Suite Only=$Only." }
    if ($List) { Show-TestList $tests; exit 0 }

    $msbuild = Resolve-MSBuild
    if (-not $SkipSolutionBuild) {
        Write-Step "Build solution Release x64"
        Assert-GlobalTimeout "solution build"
        & $msbuild ".\Chess.sln" "/restore" "/m:$maxCpu" "/nr:false" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:minimal"
        if ($LASTEXITCODE -ne 0) { throw "Solution build failed." }
    }

    $buildResults = New-Object System.Collections.Generic.List[object]
    if (-not $SkipTestBuild) {
        Write-Step "Build selected test projects"
        foreach ($item in $tests | Where-Object { -not $_.IsBenchmark }) {
            Assert-GlobalTimeout "build $($item.Name)"
            $buildResult = Invoke-BuildProject $item $msbuild $maxCpu
            $buildResults.Add($buildResult) | Out-Null
        }
    }
    else {
        foreach ($item in $tests | Where-Object { -not $_.IsBenchmark }) {
            $buildResults.Add([PSCustomObject]@{ Name = $item.Name; BuildResult = "SKIP"; BuildLog = "" }) | Out-Null
        }
    }
    $failedBuilds = @($buildResults | Where-Object { $_.BuildResult -eq "FAIL" })
    if ($failedBuilds.Count -gt 0) { Write-Step "Build failed"; exit 1 }
    if ($BuildOnly) { Write-Step "Build summary"; Write-Host "Built selected test projects: $($buildResults.Count)"; exit 0 }

    $runResults = New-Object System.Collections.Generic.List[object]
    $hadFailure = $false
    foreach ($item in $tests) {
        Assert-GlobalTimeout "run $($item.Name)"
        $raw = @(Invoke-TestExecutable $item $script:TestLogRoot)
        $runResult = $raw | Where-Object { $_.PSObject.Properties["RunResult"] } | Select-Object -Last 1
        if ($null -eq $runResult) { throw "Test runner did not produce a structured result for $($item.Name)." }
        $runResults.Add($runResult) | Out-Null
        Write-Host ("RESULT {0}: {1} exit={2} timeout={3}s" -f $runResult.Name, $runResult.RunResult, $runResult.ExitCode, $runResult.TimeoutSeconds)
        if ($runResult.RunResult -ne "PASS") { $hadFailure = $true }
    }

    Write-Step "Contract test summary"
    Write-Host "Selected tests: $(@($tests).Count)"
    Write-Host "Executed tests: $($runResults.Count)"
    if ($hadFailure) { Write-Step "Failures"; Write-Host "At least one selected test failed or timed out. See .tmp\test-logs."; exit 1 }
    exit 0
}
catch {
    Write-Error $_
    exit 1
}
finally {
    if (Test-Path variable:oldPath) { $env:PATH = $oldPath }
    Pop-Location
}
