param(
    [Parameter(Mandatory = $true)][string]$ArchivePath,
    [Parameter(Mandatory = $true)][string]$ArchiveSha256,
    [string]$SshTarget = "root@178.105.220.117",
    [string]$SshKeyPath = "",
    [Parameter(Mandatory = $true)][string]$ExpectedCommit,
    [switch]$DryRun,
    [switch]$SkipUpload,
    [switch]$RollbackOnFailure,
    [int]$HealthTimeoutSeconds = 60,
    [switch]$NoSecretLog,
    [switch]$AllowDirtyTree,
    [switch]$AllowArchiveNameMismatch
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if ([string]::IsNullOrWhiteSpace($SshKeyPath)) {
    $SshKeyPath = Join-Path $env:USERPROFILE ".ssh\id_ed25519_hetzner"
}

function Write-Step([string]$Message) {
    Write-Host "[P4K deploy] $Message"
}

function Assert-File([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file missing: $Label ($Path)"
    }
}

function Get-RelativeArchiveEntries([string]$Path) {
    $entries = & tar -tzf $Path
    if ($LASTEXITCODE -ne 0) {
        throw "Could not list archive: $Path"
    }

    return @($entries | ForEach-Object { $_.Trim().TrimStart("./") } | Where-Object { $_ -ne "" })
}

function Read-ArchiveText([string]$Path, [string[]]$Candidates) {
    foreach ($candidate in $Candidates) {
        $text = & tar -xOf $Path $candidate 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($text)) {
            return ($text -join "`n")
        }
    }

    throw "Could not read archive member. Tried: $($Candidates -join ', ')"
}

function Assert-NoForbiddenArchiveEntries([string[]]$Entries) {
    $patterns = @(
        "*.db",
        "*.sqlite",
        "*.sqlite3",
        "*.key",
        "*.pem",
        "*.pfx",
        "*password*",
        "*token*",
        "*.secret",
        "*.secrets",
        "secret.*",
        "secrets.*",
        "*keyring*",
        "key-*.xml",
        "known_hosts",
        "id_ed25519*",
        "chess3d-online-store.json"
    )

    $bad = foreach ($entry in $Entries) {
        $name = Split-Path -Leaf $entry
        foreach ($pattern in $patterns) {
            if ($name -like $pattern) {
                $entry
                break
            }
        }
    }

    if ($bad) {
        $bad | ForEach-Object { Write-Host "Forbidden archive entry: $_" }
        throw "Archive contains secret-like/runtime entries."
    }
}

function Assert-RequiredArchiveEntries([string[]]$Entries) {
    $required = @(
        "ChessOnlineServer.dll",
        "ChessOnlineProtocol.dll",
        "ChessOnlinePersistence.dll",
        "ChessOnlineServer.runtimeconfig.json",
        "ChessOnlineServer.deps.json",
        "appsettings.Production.sample.json",
        "server-build.json",
        "server-package-manifest.json",
        "libChess3DEngine.so"
    )

    foreach ($entry in $required) {
        if (-not ($Entries -contains $entry)) {
            throw "Required archive entry missing: $entry"
        }
    }

    $profileEntries = @($Entries | Where-Object {
        $_ -like "Assets/Rules3D/Profiles/*.json" -and
        (Split-Path -Leaf $_) -ne "chess3d_rule_profile.schema.json"
    })

    if ($profileEntries.Count -ne 5) {
        $profileEntries | ForEach-Object { Write-Host "Profile archive entry: $_" }
        throw "Archive must contain exactly five Chess3D RuleProfile JSON files."
    }
}

function Assert-CleanTree() {
    if ($AllowDirtyTree) {
        return
    }

    $status = & git -C $root status --short
    if ($LASTEXITCODE -ne 0) {
        throw "Could not check git status."
    }

    if (-not [string]::IsNullOrWhiteSpace(($status -join "`n"))) {
        throw "Refusing deploy from dirty local tree. Commit changes or pass -AllowDirtyTree for explicit operator override."
    }
}

function Assert-ExpectedArchiveName([string]$Path) {
    if ($AllowArchiveNameMismatch) {
        return
    }

    $name = Split-Path -Leaf $Path
    if ($name -notlike "ChessOnlineServer-P4K-*.tar.gz") {
        throw "Archive name does not match expected P4K pattern: $name"
    }
}

function Assert-ArchiveSha([string]$Path, [string]$Expected) {
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
    $expectedUpper = $Expected.Trim().ToUpperInvariant()
    if ($actual -ne $expectedUpper) {
        throw "Archive SHA-256 mismatch. Expected $expectedUpper, got $actual"
    }

    return $actual
}

function Get-BashSingleQuoted([string]$Value) {
    return "'" + $Value.Replace("'", "'\''") + "'"
}

function Invoke-SshBash([string]$Script, [string[]]$Arguments) {
    $quotedArgs = $Arguments | ForEach-Object { Get-BashSingleQuoted $_ }
    $remoteCommand = "bash -s -- $($quotedArgs -join ' ')"
    $sshArgs = @("-i", $SshKeyPath, $SshTarget, $remoteCommand)
    $Script | & ssh @sshArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Remote SSH command failed with exit code $LASTEXITCODE."
    }
}

function Invoke-ScpUpload([string]$LocalPath, [string]$RemotePath) {
    & scp -i $SshKeyPath $LocalPath "${SshTarget}:$RemotePath"
    if ($LASTEXITCODE -ne 0) {
        throw "SCP upload failed with exit code $LASTEXITCODE."
    }
}

$ArchivePath = [System.IO.Path]::GetFullPath($ArchivePath)
Assert-File $ArchivePath "archive"
Assert-File $SshKeyPath "SSH key"
Assert-CleanTree
Assert-ExpectedArchiveName $ArchivePath

$archiveSha = Assert-ArchiveSha $ArchivePath $ArchiveSha256
$entries = Get-RelativeArchiveEntries $ArchivePath
Assert-NoForbiddenArchiveEntries $entries
Assert-RequiredArchiveEntries $entries

$buildJson = Read-ArchiveText $ArchivePath @("./server-build.json", "server-build.json")
$buildIdentity = $buildJson | ConvertFrom-Json
if ($buildIdentity.commit -ne $ExpectedCommit) {
    throw "Archive commit mismatch. Expected $ExpectedCommit, got $($buildIdentity.commit)"
}

$packageId = [string]$buildIdentity.packageId
if ([string]::IsNullOrWhiteSpace($packageId)) {
    throw "Archive server-build.json does not contain packageId."
}

$archiveName = Split-Path -Leaf $ArchivePath
$remoteArchive = "/opt/chessonline/incoming/$archiveName"

Write-Step "Archive: $ArchivePath"
Write-Step "Archive SHA-256: $archiveSha"
Write-Step "Expected commit: $ExpectedCommit"
Write-Step "Package id: $packageId"
Write-Step "Remote target: $SshTarget"
Write-Step "Remote archive: $remoteArchive"
Write-Step "Health timeout: $HealthTimeoutSeconds seconds"
Write-Step "NoSecretLog: $([bool]$NoSecretLog)"

if ($DryRun) {
    Write-Step "DRY RUN ONLY: no SSH mutation, no SCP upload, no service stop/start."
    Write-Step "Planned upload: $remoteArchive"
    Write-Step "Planned extract: /opt/chessonline/server.new.<timestamp>"
    Write-Step "Planned stop/start: chessonline.service only"
    Write-Step "Planned health checks: http://127.0.0.1:5077/healthz/live, /healthz/ready, /chess3d/diagnostics, public HTTP equivalents"
    Write-Step "Expected capabilities after deploy: resumeMatch, spectatorMode, lobbySnapshot, RequestResumeMatch, JoinSpectator, RequestLobbySnapshot"
    Write-Step "Planned rollback: restore previous /opt/chessonline/server directory when -RollbackOnFailure is set"
    exit 0
}

$remotePrepare = @'
set -euo pipefail
archive="$1"
archive_sha="$2"
install -d -m 700 /opt/chessonline/incoming
if [ -f "$archive" ]; then
  actual=$(sha256sum "$archive" | awk '{print $1}')
  if [ "$actual" != "$archive_sha" ]; then
    echo "Existing remote archive checksum mismatch: $actual" >&2
    exit 20
  fi
fi
echo "remote incoming ready"
'@

Invoke-SshBash $remotePrepare @($remoteArchive, $archiveSha.ToLowerInvariant())

if (-not $SkipUpload) {
    Write-Step "Uploading archive..."
    Invoke-ScpUpload $ArchivePath $remoteArchive
}
else {
    Write-Step "SkipUpload set; expecting archive already present remotely."
}

$remoteDeploy = @'
set -euo pipefail
archive="$1"
archive_sha="$2"
expected_commit="$3"
health_timeout="$4"
rollback_on_failure="$5"

service=chessonline.service
server_root=/opt/chessonline
current_dir=$server_root/server
timestamp=$(date -u +%Y%m%d-%H%M%S)
new_dir=$server_root/server.new.$timestamp
prev_dir=$server_root/server.prev.$timestamp

rollback() {
  if [ "$rollback_on_failure" = "true" ] && [ -d "$prev_dir" ]; then
    echo "rollback: restoring previous server directory"
    systemctl stop "$service" || true
    rm -rf "$current_dir"
    mv "$prev_dir" "$current_dir"
    systemctl start "$service"
  fi
}
trap rollback ERR

actual=$(sha256sum "$archive" | awk '{print $1}')
if [ "$actual" != "$archive_sha" ]; then
  echo "remote checksum mismatch: $actual" >&2
  exit 21
fi

rm -rf "$new_dir"
mkdir -p "$new_dir"
tar -xzf "$archive" -C "$new_dir"

test -f "$new_dir/ChessOnlineServer.dll"
test -f "$new_dir/ChessOnlineProtocol.dll"
test -f "$new_dir/libChess3DEngine.so"
test -f "$new_dir/server-build.json"
profile_count=$(find "$new_dir/Assets/Rules3D/Profiles" -maxdepth 1 -type f -name '*.json' ! -name 'chess3d_rule_profile.schema.json' | wc -l)
if [ "$profile_count" != "5" ]; then
  echo "expected exactly five profile JSON files, got $profile_count" >&2
  exit 22
fi

if ! grep -Fq "$expected_commit" "$new_dir/server-build.json"; then
  echo "expected commit not found in server-build.json" >&2
  exit 23
fi

chown -R chessonline:chessonline "$new_dir"

systemctl stop "$service"
mv "$current_dir" "$prev_dir"
mv "$new_dir" "$current_dir"
systemctl start "$service"

deadline=$((SECONDS + health_timeout))
until curl -fsS http://127.0.0.1:5077/healthz/live >/dev/null; do
  if [ "$SECONDS" -ge "$deadline" ]; then
    echo "loopback live health timed out" >&2
    exit 24
  fi
  sleep 2
done

curl -fsS http://127.0.0.1:5077/healthz/ready >/dev/null
diagnostics=$(curl -fsS http://127.0.0.1:5077/chess3d/diagnostics)
echo "$diagnostics" | grep -Fq "$expected_commit"
echo "$diagnostics" | grep -Fq "RequestResumeMatch"
echo "$diagnostics" | grep -Fq "JoinSpectator"
echo "$diagnostics" | grep -Fq "RequestLobbySnapshot"

echo "deploy complete"
echo "previous_dir=$prev_dir"
'@

Invoke-SshBash $remoteDeploy @(
    $remoteArchive,
    $archiveSha.ToLowerInvariant(),
    $ExpectedCommit,
    ([string]$HealthTimeoutSeconds),
    ($(if ($RollbackOnFailure) { "true" } else { "false" }))
)

Write-Step "Deploy command completed."
