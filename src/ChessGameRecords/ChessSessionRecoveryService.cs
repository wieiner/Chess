namespace ChessGameRecords;

public sealed record ChessRecoveryCandidate(string Path, ChessSessionDocument Document, string Hash);

public sealed class ChessSessionRecoveryService
{
    private readonly string _directory;
    private readonly int _retention;
    private readonly ChessSessionFileService _files;

    public ChessSessionRecoveryService(string directory, int retention = 8, ChessSessionFileService? files = null)
    {
        _directory = Path.GetFullPath(directory);
        _retention = Math.Clamp(retention, 1, 64);
        _files = files ?? new ChessSessionFileService();
    }

    public static string DefaultDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chess", "Autosave");

    public ChessSessionFileResult SaveAutosave(ChessSessionDocument source, long sequence)
    {
        if (sequence < 1) return new(false, null, string.Empty, "Autosave sequence must be positive.");
        var now = DateTimeOffset.UtcNow;
        var autosave = source with
        {
            ModifiedUtc = now,
            Dirty = true,
            Autosave = new ChessSessionAutosaveMetadata(true, source.SessionId, sequence, now)
        };
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, $"{source.SessionId:N}-{sequence:D12}.chesssession.json");
        var result = _files.Save(path, autosave, keepBackup: false);
        if (result.Success) CleanupOldFiles(source.SessionId);
        return result;
    }

    public IReadOnlyList<ChessRecoveryCandidate> GetCandidates(Guid? sessionId = null)
    {
        if (!Directory.Exists(_directory)) return Array.Empty<ChessRecoveryCandidate>();
        var candidates = new List<ChessRecoveryCandidate>();
        foreach (var path in Directory.EnumerateFiles(_directory, "*.chesssession.json", SearchOption.TopDirectoryOnly))
        {
            var loaded = _files.Load(path);
            if (!loaded.Success || loaded.Document?.Autosave is not { IsAutosave: true } autosave) continue;
            if (sessionId is not null && autosave.SourceSessionId != sessionId) continue;
            candidates.Add(new ChessRecoveryCandidate(path, loaded.Document, loaded.Hash));
        }
        return candidates
            .OrderByDescending(candidate => candidate.Document.Autosave!.SavedUtc)
            .ThenByDescending(candidate => candidate.Document.Autosave!.Sequence)
            .ToArray();
    }

    public ChessRecoveryCandidate? GetLatestCandidate(string? explicitSessionPath = null)
    {
        ChessSessionDocument? explicitDocument = null;
        if (!string.IsNullOrWhiteSpace(explicitSessionPath))
        {
            var explicitLoad = _files.Load(explicitSessionPath);
            explicitDocument = explicitLoad.Success ? explicitLoad.Document : null;
        }
        return GetCandidates(explicitDocument?.SessionId)
            .FirstOrDefault(candidate => explicitDocument is null || candidate.Document.ModifiedUtc > explicitDocument.ModifiedUtc);
    }

    public void Discard(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!IsWithinDirectory(fullPath) || !File.Exists(fullPath)) return;
        File.Delete(fullPath);
    }

    public void DiscardForSession(Guid sessionId)
    {
        foreach (var candidate in GetCandidates(sessionId)) Discard(candidate.Path);
    }

    public static bool ShouldScheduleAfterAction(bool accepted) => accepted;

    private void CleanupOldFiles(Guid sessionId)
    {
        foreach (var candidate in GetCandidates(sessionId).Skip(_retention)) Discard(candidate.Path);
    }

    private bool IsWithinDirectory(string path)
    {
        var relative = Path.GetRelativePath(_directory, path);
        return relative != ".." && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
            !Path.IsPathRooted(relative);
    }
}
