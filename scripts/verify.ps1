param(
    [switch]$SkipBenchmark,
    [int]$MSBuildMaxCpuCount = 0,
    [int]$TestTimeoutSeconds = 120,
    [int]$OnlineTestTimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir ".."))
$Configuration = "Release"
$Platform = "x64"
$ResolvedMSBuildMaxCpuCount = if ($MSBuildMaxCpuCount -gt 0) {
    $MSBuildMaxCpuCount
}
elseif ($env:CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT -match '^[1-9][0-9]*$') {
    [int]$env:CHESS_VERIFY_MSBUILD_MAX_CPU_COUNT
}
else {
    4
}

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
    Write-Host "MSBuild max CPU count: $ResolvedMSBuildMaxCpuCount"
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

    Write-Step "Visual asset catalog"
    Assert-File "assets\models\chess\pieces\piece_sets.json"
    Assert-File "assets\models\chess\pieces\default\Pieces\white_pawn.obj"
    Assert-File "assets\models\chess\pieces\default\Pieces\white_pawn.mtl"
    Assert-File "assets\models\chess\pieces\default\Pieces\black_pawn.obj"
    Assert-File "assets\models\chess\pieces\default\Pieces\black_pawn.mtl"
    Assert-File "assets\models\chess\pieces\default\Board\light_tile.obj"
    Assert-File "assets\models\chess\pieces\generated\piece_set.generated.example.json"

    Write-Step "Windows server deployment scripts"
    Assert-File "scripts\deploy\Start-ChessOnlineServer-Windows.ps1"
    Assert-File "scripts\deploy\Stop-ChessOnlineServer-Windows.ps1"
    Assert-File "scripts\deploy\Test-ChessOnlineServer-Windows.ps1"
    Assert-File "deploy\windows\README.md"
    Assert-File "deploy\windows\install-chessonline-server.ps1.template"
    Assert-File "deploy\windows\uninstall-chessonline-server.ps1.template"
    Assert-File "docs\CHESS3D_WINDOWS_SERVER_RUNBOOK.md"
    Assert-File "docs\CHESS3D_GENERATED_PIECE_ASSET_PIPELINE.md"
    Assert-File "docs\presentation\CHESS3D_PRODUCT_PRESENTATION.md"
    Assert-File "docs\presentation\CHESS3D_FEATURE_INVENTORY.md"
    Assert-File "docs\presentation\CHESS3D_SCREENSHOT_TODO.md"
    Assert-File "docs\presentation\chess3d_presentation.html"
    Assert-File "docs\CHESS3D_DEPLOYMENT_DECISION_PACKAGE.md"
    Assert-File "docs\CHESS3D_HETZNER_ACTION_PLAN.md"
    Assert-File "docs\CHESS3D_DEPLOYMENT_CHECKLIST.md"
    Assert-File "docs\CHESS3D_ONLINE_AUTHORITY_ADAPTER.md"
    Assert-File "docs\CHESS3D_MATCHMAKING_DURABILITY_AUDIT.md"
    Assert-File "docs\CHESS3D_ASGARD_DEEPENING_PLAN.md"
    Assert-File "docs\CHESS3D_P4D_LINUX_NATIVE_AUTHORITY_PLAN.md"
    Assert-File "docs\CHESS3D_CLANG_LINUX_TOOLCHAIN_PLAN.md"
    Assert-File "docs\CHESS3D_HETZNER_BUILD_PROBE_PLAN.md"
    Assert-File "cmake\toolchains\linux-x64-clang-from-windows.cmake"

    Write-Step "Build Release x64"
    $msbuild = Resolve-MSBuild
    Invoke-Checked { & $msbuild ".\Chess.sln" "/restore" "/m:$ResolvedMSBuildMaxCpuCount" "/nr:false" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:minimal" } "MSBuild failed."

    Write-Step "Development executable checks"
    Assert-File "src\ChessApp\bin\x64\Release\net8.0-windows\ChessApp.exe"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Chess3DApp.exe"
    Assert-File "src\RubikApp\bin\x64\Release\net8.0-windows\RubikApp.exe"
    Assert-File "src\ChessOnlineApp\bin\x64\Release\net8.0-windows\ChessOnlineApp.exe"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\ChessOnlineServer.exe"
    Assert-File "src\ChessOnlineApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Profiles\classic_six_side_3d_v0_1.json"
    Assert-File "src\ChessOnlineApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Profiles\hodge_projection_duel_3d_v0_1.json"
    Assert-File "src\ChessOnlineApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Online\schemas\chess3d_relay_v0_1.schema.json"
    Assert-File "src\ChessOnlineApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\OnlineScenarios\online_protocol_hello_v0_1.json"
    Assert-File "src\ChessOnlineApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\OnlineScenarios\online_hodge_composite_smoke_v0_1.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Assets\Rules3D\Profiles\classic_six_side_3d_v0_1.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Assets\Rules3D\Online\schemas\chess3d_relay_v0_1.schema.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Assets\Rules3D\SignalRScenarios\signalr_hello_connect_v0_1.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Assets\Rules3D\IdentityScenarios\identity_register_login_v0_1.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Assets\Rules3D\PersistenceScenarios\persistence_room_table_action_log_v0_1.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Assets\Rules3D\MatchmakingScenarios\matchmaking_classic_match_found_v0_1.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Assets\Rules3D\AsgardOnlineScenarios\asgard_matchmaking_table_v0_1.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Assets\Rules3D\DeploymentScenarios\deployment_nginx_template_v0_1.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\appsettings.Production.sample.json"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Deploy\linux\chessonline-server.service.template"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Deploy\linux\nginx-chessonline.conf.template"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Deploy\windows\README.md"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Deploy\windows\install-chessonline-server.ps1.template"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Deploy\windows\uninstall-chessonline-server.ps1.template"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Deploy\windows\Start-ChessOnlineServer-Windows.ps1"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Deploy\windows\Stop-ChessOnlineServer-Windows.ps1"
    Assert-File "src\ChessOnlineServer\bin\x64\Release\net8.0\Deploy\windows\Test-ChessOnlineServer-Windows.ps1"
    Assert-File "bin\x64\Release\Chess2DBenchmark.exe"
    Assert-File "src\ChessApp\bin\x64\Release\net8.0-windows\Assets\Models\piece_sets.json"
    Assert-File "src\ChessApp\bin\x64\Release\net8.0-windows\Assets\Models\default\Pieces\white_pawn.obj"
    Assert-File "src\ChessApp\bin\x64\Release\net8.0-windows\Assets\Models\default\Pieces\black_pawn.mtl"
    Assert-File "src\ChessApp\bin\x64\Release\net8.0-windows\Assets\Models\generated\piece_set.generated.example.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Models\piece_sets.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Models\default\Pieces\white_pawn.obj"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Models\default\Pieces\black_pawn.mtl"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Models\generated\piece_set.generated.example.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Profiles\asgard_convergence_3d_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Profiles\rubik_convergence_3d_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Profiles\hodge_projection_duel_3d_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\classic_six_side_smoke_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\asgard_core_fusion_smoke_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\rubik_layer_turn_smoke_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\hodge_projection_smoke_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\classic_six_side_playthrough_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\single_side_training_playthrough_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\asgard_core_playthrough_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\rubik_layer_playthrough_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\hodge_projection_playthrough_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\invalid_click_no_mutation_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\rubik_four_turn_roundtrip_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\hodge_blocked_mirror_rollback_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\asgard_stack_fusion_anchor_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\classic_turn_progression_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\classic_self_check_illegal_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\classic_king_cannot_move_into_check_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\classic_capture_checker_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\classic_block_sliding_check_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\classic_checkmate_micro_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\classic_stalemate_micro_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\single_side_king_safety_smoke_v0_1.json"
    Assert-File "src\Chess3DApp\bin\x64\Release\net8.0-windows\Assets\Rules3D\Scenarios\regression\non_classic_outcome_isolation_v0_1.json"

    Write-Step "Production packaging"
    Invoke-Checked { powershell -NoProfile -ExecutionPolicy Bypass -File ".\tools\release\Build-Production.ps1" -Product All } "Production packaging failed."

    Write-Step "Portable executable checks"
    Assert-File "ProductionOutput\Chess2D\ChessApp.exe"
    Assert-File "ProductionOutput\Chess3D\Chess3DApp.exe"
    Assert-File "ProductionOutput\Rubik\RubikApp.exe"
    Assert-File "ProductionOutput\ChessOnlineIntegrations\ChessOnlineApp.exe"
    Assert-File "ProductionOutput\ChessOnlineServer\ChessOnlineServer.exe"
    Assert-File "ProductionOutput\ChessOnlineIntegrations\Assets\Rules3D\Profiles\classic_six_side_3d_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineIntegrations\Assets\Rules3D\Profiles\hodge_projection_duel_3d_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineIntegrations\Assets\Rules3D\Online\schemas\chess3d_relay_v0_1.schema.json"
    Assert-File "ProductionOutput\ChessOnlineIntegrations\Assets\Rules3D\OnlineScenarios\online_protocol_hello_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineIntegrations\Assets\Rules3D\OnlineScenarios\online_hodge_composite_smoke_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Assets\Rules3D\Profiles\classic_six_side_3d_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Assets\Rules3D\Online\schemas\chess3d_relay_v0_1.schema.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Assets\Rules3D\SignalRScenarios\signalr_hello_connect_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Assets\Rules3D\IdentityScenarios\identity_register_login_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Assets\Rules3D\PersistenceScenarios\persistence_room_table_action_log_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Assets\Rules3D\MatchmakingScenarios\matchmaking_classic_match_found_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Assets\Rules3D\AsgardOnlineScenarios\asgard_matchmaking_table_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Assets\Rules3D\DeploymentScenarios\deployment_nginx_template_v0_1.json"
    Assert-File "ProductionOutput\ChessOnlineServer\appsettings.Production.sample.json"
    Assert-File "ProductionOutput\ChessOnlineServer\Deploy\linux\chessonline-server.service.template"
    Assert-File "ProductionOutput\ChessOnlineServer\Deploy\linux\nginx-chessonline.conf.template"
    Assert-File "ProductionOutput\ChessOnlineServer\Deploy\windows\README.md"
    Assert-File "ProductionOutput\ChessOnlineServer\Deploy\windows\install-chessonline-server.ps1.template"
    Assert-File "ProductionOutput\ChessOnlineServer\Deploy\windows\uninstall-chessonline-server.ps1.template"
    Assert-File "ProductionOutput\ChessOnlineServer\Deploy\windows\Start-ChessOnlineServer-Windows.ps1"
    Assert-File "ProductionOutput\ChessOnlineServer\Deploy\windows\Stop-ChessOnlineServer-Windows.ps1"
    Assert-File "ProductionOutput\ChessOnlineServer\Deploy\windows\Test-ChessOnlineServer-Windows.ps1"
    Assert-File "ProductionOutput\Chess2DBenchmark\Chess2DBenchmark.exe"
    Assert-File "ProductionOutput\Chess2D\Assets\Models\piece_sets.json"
    Assert-File "ProductionOutput\Chess2D\Assets\Models\default\Pieces\white_pawn.obj"
    Assert-File "ProductionOutput\Chess2D\Assets\Models\default\Pieces\black_pawn.mtl"
    Assert-File "ProductionOutput\Chess2D\Assets\Models\generated\piece_set.generated.example.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Models\piece_sets.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Models\default\Pieces\white_pawn.obj"
    Assert-File "ProductionOutput\Chess3D\Assets\Models\default\Pieces\black_pawn.mtl"
    Assert-File "ProductionOutput\Chess3D\Assets\Models\generated\piece_set.generated.example.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Profiles\asgard_convergence_3d_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Profiles\rubik_convergence_3d_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Profiles\hodge_projection_duel_3d_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\classic_six_side_smoke_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\asgard_core_fusion_smoke_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\rubik_layer_turn_smoke_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\hodge_projection_smoke_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\classic_six_side_playthrough_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\single_side_training_playthrough_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\asgard_core_playthrough_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\rubik_layer_playthrough_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\hodge_projection_playthrough_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\invalid_click_no_mutation_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\rubik_four_turn_roundtrip_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\hodge_blocked_mirror_rollback_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\asgard_stack_fusion_anchor_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\classic_turn_progression_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\classic_self_check_illegal_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\classic_king_cannot_move_into_check_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\classic_capture_checker_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\classic_block_sliding_check_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\classic_checkmate_micro_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\classic_stalemate_micro_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\single_side_king_safety_smoke_v0_1.json"
    Assert-File "ProductionOutput\Chess3D\Assets\Rules3D\Scenarios\regression\non_classic_outcome_isolation_v0_1.json"

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

    $secretExtensions = @(".db", ".sqlite", ".sqlite3", ".key", ".pfx", ".pem")
    $secretNamePatterns = @("*.secrets", "*password*", "*token*", "key-*.xml", "chess3d-online-store.json")
    $secretPortableFiles = Get-ChildItem -LiteralPath (Join-Path $Root "ProductionOutput") -Recurse -File | Where-Object {
        $extension = $_.Extension.ToLowerInvariant()
        if ($secretExtensions -contains $extension) {
            return $true
        }
        foreach ($pattern in $secretNamePatterns) {
            if ($_.Name -like $pattern) {
                return $true
            }
        }
        return $false
    }
    if ($secretPortableFiles) {
        $secretPortableFiles | Select-Object -ExpandProperty FullName
        throw "ProductionOutput contains runtime secret, database, token, or key artifacts."
    }

    Write-Step "Contract tests"
    $testScript = Join-Path $Root "tests\run-tests.ps1"
    if (-not (Test-Path -LiteralPath $testScript -PathType Leaf)) {
        throw "Contract test runner is missing: $testScript"
    }
    if ($SkipBenchmark) {
        Invoke-Checked { powershell -NoProfile -ExecutionPolicy Bypass -File ".\tests\run-tests.ps1" -SkipSolutionBuild -Suite All -MSBuildMaxCpuCount $ResolvedMSBuildMaxCpuCount -TestTimeoutSeconds $TestTimeoutSeconds -OnlineTestTimeoutSeconds $OnlineTestTimeoutSeconds -SkipBenchmark } "Contract tests failed."
    }
    else {
        Invoke-Checked { powershell -NoProfile -ExecutionPolicy Bypass -File ".\tests\run-tests.ps1" -SkipSolutionBuild -Suite All -MSBuildMaxCpuCount $ResolvedMSBuildMaxCpuCount -TestTimeoutSeconds $TestTimeoutSeconds -OnlineTestTimeoutSeconds $OnlineTestTimeoutSeconds } "Contract tests failed."
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
