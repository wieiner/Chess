param(
    [string]$RepositoryRoot = "",
    [switch]$IncludeAllDotnet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DefaultRoot = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir "..\.."))
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = $DefaultRoot
}
$RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
$escapedRoot = [Regex]::Escape($RepositoryRoot)

$names = @("MSBuild.exe", "VBCSCompiler.exe", "dotnet.exe")
$processes = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -in $names
})

$processes |
    ForEach-Object {
        $commandLine = [string]$_.CommandLine
        $isRepoRelated = $commandLine -match $escapedRoot
        $include = $_.Name -ne "dotnet.exe" -or $IncludeAllDotnet -or $isRepoRelated
        if (-not $include) { return }
        [PSCustomObject]@{
            ProcessId = $_.ProcessId
            Name = $_.Name
            CreationDate = $_.CreationDate
            RepoRelated = $isRepoRelated
            CommandLine = $commandLine
        }
    } |
    Sort-Object Name, ProcessId |
    Format-Table -AutoSize
