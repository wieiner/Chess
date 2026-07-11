param(
    [string]$Configuration = "Release",
    [string]$Runtime = "linux-x64",
    [string]$OutputPath = "",
    [string]$NativeLibraryPath = "",
    [string]$CommitSha = "",
    [string]$PackageId = "",
    [string]$ManifestPath = "",
    [switch]$Clean,
    [switch]$FailOnSecretLikeFiles
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$project = Join-Path $root "src\ChessOnlineServer\ChessOnlineServer.csproj"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root "DeploymentOutput\$Runtime\ChessOnlineServer"
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)

function Resolve-UnderRoot([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $rootFull = [System.IO.Path]::GetFullPath($root)
    if (-not $rootFull.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $rootFull += [System.IO.Path]::DirectorySeparatorChar
    }

    if (-not ($full.Equals($rootFull.TrimEnd([System.IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase) -or
              $full.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase))) {
        throw "Refusing to operate outside repository root: $full"
    }

    return $full
}

function Assert-File([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file missing: $Label ($Path)"
    }
}

function Get-RelativePath([string]$BasePath, [string]$FullPath) {
    [System.IO.Path]::GetRelativePath($BasePath, $FullPath).Replace('\', '/')
}

function Assert-NoSecretLikeFiles([string]$Directory) {
    $secretExtensions = @(".db", ".sqlite", ".sqlite3", ".key", ".pem", ".pfx")
    $secretNamePatterns = @(
        "*password*",
        "*token*",
        "*.secret",
        "*.secrets",
        "secret.*",
        "secrets.*",
        "*keyring*",
        "key-*.xml",
        "chess3d-online-store.json",
        "known_hosts",
        "id_ed25519*"
    )

    $bad = Get-ChildItem -LiteralPath $Directory -Recurse -File | Where-Object {
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

    if ($bad) {
        $bad | ForEach-Object { Write-Host "Forbidden package file: $(Get-RelativePath $Directory $_.FullName)" }
        throw "Linux server package contains secret-like/runtime files."
    }
}

function Write-PackageManifest([string]$Directory, [string]$Path) {
    $manifestFull = [System.IO.Path]::GetFullPath($Path)
    $manifestParent = Split-Path -Parent $manifestFull
    if (-not (Test-Path -LiteralPath $manifestParent)) {
        New-Item -ItemType Directory -Path $manifestParent | Out-Null
    }

    $files = Get-ChildItem -LiteralPath $Directory -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            [ordered]@{
                path = Get-RelativePath $Directory $_.FullName
                length = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }

    $manifest = [ordered]@{
        format = "chessonline-server-package-manifest"
        version = "0.1"
        packageId = $PackageId
        commit = $CommitSha
        runtime = $Runtime
        createdUtc = [DateTime]::UtcNow.ToString("O")
        fileCount = @($files).Count
        files = @($files)
    }

    $manifest |
        ConvertTo-Json -Depth 6 |
        Set-Content -LiteralPath $manifestFull -Encoding UTF8
}

function Remove-NonDeployFiles([string]$Directory) {
    $dropNames = @(
        "appsettings.Development.json",
        "appsettings.Local.json"
    )

    foreach ($name in $dropNames) {
        $path = Join-Path $Directory $name
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    Get-ChildItem -LiteralPath $Directory -Recurse -File -Filter "*.pdb" -ErrorAction SilentlyContinue |
        Remove-Item -Force
}

$outputUnderRoot = Resolve-UnderRoot $OutputPath
if ($Clean) {
    $tmpRoot = Join-Path ([System.IO.Path]::GetFullPath($root)) ".tmp"
    if (-not ($outputUnderRoot.StartsWith($tmpRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase))) {
        throw "-Clean is allowed only for output paths under .tmp/: $outputUnderRoot"
    }

    if (Test-Path -LiteralPath $outputUnderRoot) {
        Remove-Item -LiteralPath $outputUnderRoot -Recurse -Force
    }
}

if (-not [string]::IsNullOrWhiteSpace($NativeLibraryPath)) {
    if (-not (Test-Path -LiteralPath $NativeLibraryPath)) {
        throw "Linux native library not found: $NativeLibraryPath"
    }

    dotnet publish $project -c $Configuration -r $Runtime --self-contained false -p:Platform=x64 -p:Chess3DEngineLinuxPath="$NativeLibraryPath" -o $OutputPath
    Copy-Item -LiteralPath $NativeLibraryPath -Destination (Join-Path $OutputPath "libChess3DEngine.so") -Force
    Get-ChildItem -LiteralPath $OutputPath -Filter "libChess3DEngine*.so" -File |
        Where-Object { $_.Name -ne "libChess3DEngine.so" } |
        Remove-Item -Force
}
else {
    dotnet publish $project -c $Configuration -r $Runtime --self-contained false -p:Platform=x64 -o $OutputPath
}

Remove-NonDeployFiles $OutputPath

if ([string]::IsNullOrWhiteSpace($CommitSha)) {
    try {
        $CommitSha = (& git -C $root rev-parse HEAD 2>$null).Trim()
    }
    catch {
        $CommitSha = ""
    }
}

if ([string]::IsNullOrWhiteSpace($PackageId)) {
    $short = if ($CommitSha.Length -ge 12) { $CommitSha.Substring(0, 12) } else { "unknown" }
    $PackageId = "chessonline-$Runtime-$short"
}

$buildIdentity = [ordered]@{
    commit = $CommitSha
    builtUtc = [DateTime]::UtcNow.ToString("O")
    packageId = $PackageId
    informationalVersion = ""
}

$buildIdentity |
    ConvertTo-Json -Depth 3 |
    Set-Content -LiteralPath (Join-Path $OutputPath "server-build.json") -Encoding UTF8

$serverDll = Join-Path $OutputPath "ChessOnlineServer.dll"
Assert-File $serverDll "ChessOnlineServer.dll"
Assert-File (Join-Path $OutputPath "ChessOnlineProtocol.dll") "ChessOnlineProtocol.dll"
Assert-File (Join-Path $OutputPath "ChessOnlinePersistence.dll") "ChessOnlinePersistence.dll"
Assert-File (Join-Path $OutputPath "ChessOnlineServer.runtimeconfig.json") "ChessOnlineServer.runtimeconfig.json"
Assert-File (Join-Path $OutputPath "ChessOnlineServer.deps.json") "ChessOnlineServer.deps.json"
Assert-File (Join-Path $OutputPath "appsettings.Production.sample.json") "appsettings.Production.sample.json"
Assert-File (Join-Path $OutputPath "server-build.json") "server-build.json"

$profileRoot = Join-Path $OutputPath "Assets\Rules3D\Profiles"
if (-not (Test-Path -LiteralPath $profileRoot -PathType Container)) {
    throw "Profile output directory missing: $profileRoot"
}

$profiles = Get-ChildItem -LiteralPath $profileRoot -Filter "*.json" -File |
    Where-Object { $_.Name -ne "chess3d_rule_profile.schema.json" }
if (@($profiles).Count -ne 5) {
    $profiles | Select-Object -ExpandProperty Name
    throw "Linux package must contain exactly five Chess3D rule profile JSON files."
}

if (-not [string]::IsNullOrWhiteSpace($NativeLibraryPath)) {
    $linuxNative = Join-Path $OutputPath "libChess3DEngine.so"
    Assert-File $linuxNative "libChess3DEngine.so"
}

$windowsNative = Get-ChildItem -LiteralPath $OutputPath -Recurse -File -Filter "Chess3DEngine.dll" -ErrorAction SilentlyContinue
if ($windowsNative) {
    $windowsNative | ForEach-Object { Write-Host "Unexpected Windows native file: $(Get-RelativePath $OutputPath $_.FullName)" }
    throw "Linux server package must not include Windows Chess3DEngine.dll."
}

if ($FailOnSecretLikeFiles) {
    Assert-NoSecretLikeFiles $OutputPath
}

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $OutputPath "server-package-manifest.json"
}

Write-PackageManifest $OutputPath $ManifestPath
Write-Host "Published ChessOnlineServer Linux package: $OutputPath"
Write-Host "Manifest: $ManifestPath"
