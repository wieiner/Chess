namespace RubikState;

public enum RubikFileErrorCode
{
    None,
    AccessDenied,
    PathInvalid,
    InputTooLarge,
    MalformedDocument,
    UnsupportedVersion,
    ValidationFailed,
    HashMismatch,
    ReplaceFailed,
    DiskFull,
    Cancelled,
    InternalError
}

public enum RubikFileWriteStage
{
    BeforeTempCreate,
    AfterTempCreate,
    AfterWrite,
    AfterFlush,
    AfterValidate,
    BeforeReplace
}

public interface IRubikFileFailureInjector
{
    void AtStage(RubikFileWriteStage stage);
}

public sealed record RubikFileOperationResult(
    bool Success,
    RubikFileErrorCode ErrorCode,
    string Message,
    string Path,
    RubikStateLoadPlan? LoadPlan = null,
    string? BackupPath = null)
{
    public static RubikFileOperationResult Ok(string path, RubikStateLoadPlan? plan = null, string? backupPath = null) =>
        new(true, RubikFileErrorCode.None, string.Empty, path, plan, backupPath);
}

public sealed class RubikStateFileService
{
    public RubikFileOperationResult Read(string path, int maximumBytes = RubikStateSerializer.DefaultMaximumBytes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = NormalizePath(path);
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 64 * 1024, FileOptions.SequentialScan);
            if (stream.Length > maximumBytes)
                return Failure(RubikFileErrorCode.InputTooLarge, fullPath, $"File is {stream.Length} bytes; limit is {maximumBytes}.");

            var bytes = new byte[checked((int)stream.Length)];
            var offset = 0;
            while (offset < bytes.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = stream.Read(bytes, offset, bytes.Length - offset);
                if (count == 0)
                    throw new EndOfStreamException("Rubik state file ended before the advertised length.");
                offset += count;
            }

            var parsed = RubikStateSerializer.Parse(bytes, maximumBytes);
            if (!parsed.Success)
                return FromStateIssues(fullPath, parsed.Issues);
            return RubikFileOperationResult.Ok(fullPath, parsed.Plan);
        }
        catch (Exception exception)
        {
            return FromException(path, exception, replacing: false);
        }
    }

    public RubikFileOperationResult Save(string path, RubikStateDocument document, bool retainBackup = false,
        CancellationToken cancellationToken = default, IRubikFileFailureInjector? failureInjector = null)
    {
        string fullPath;
        try { fullPath = NormalizePath(path); }
        catch (Exception exception) { return FromException(path, exception, replacing: false); }

        var directory = Path.GetDirectoryName(fullPath)!;
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        var backupPath = retainBackup ? fullPath + ".bak" : null;
        var replacing = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Destination directory does not exist: {directory}");
            var bytes = RubikStateSerializer.SerializeToUtf8(document);

            failureInjector?.AtStage(RubikFileWriteStage.BeforeTempCreate);
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       bufferSize: 64 * 1024, FileOptions.SequentialScan))
            {
                failureInjector?.AtStage(RubikFileWriteStage.AfterTempCreate);
                stream.Write(bytes);
                failureInjector?.AtStage(RubikFileWriteStage.AfterWrite);
                stream.Flush(flushToDisk: true);
                failureInjector?.AtStage(RubikFileWriteStage.AfterFlush);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var tempRead = Read(temporaryPath, RubikStateSerializer.DefaultMaximumBytes, cancellationToken);
            if (!tempRead.Success)
                return Failure(tempRead.ErrorCode, fullPath, $"Temporary file validation failed: {tempRead.Message}");
            failureInjector?.AtStage(RubikFileWriteStage.AfterValidate);
            failureInjector?.AtStage(RubikFileWriteStage.BeforeReplace);

            replacing = true;
            if (File.Exists(fullPath))
            {
                File.Replace(temporaryPath, fullPath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, fullPath);
            }
            replacing = false;
            return RubikFileOperationResult.Ok(fullPath, tempRead.LoadPlan, backupPath);
        }
        catch (Exception exception)
        {
            return FromException(fullPath, exception, replacing);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch { /* A failed cleanup must not obscure the primary result. */ }
        }
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A non-empty file path is required.", nameof(path));
        return Path.GetFullPath(path);
    }

    private static RubikFileOperationResult FromStateIssues(string path, IReadOnlyList<RubikStateIssue> issues)
    {
        var code = issues.Any(issue => issue.Code == RubikStateErrorCode.HashMismatch) ? RubikFileErrorCode.HashMismatch
            : issues.Any(issue => issue.Code == RubikStateErrorCode.UnsupportedVersion) ? RubikFileErrorCode.UnsupportedVersion
            : issues.Any(issue => issue.Code == RubikStateErrorCode.InputTooLarge) ? RubikFileErrorCode.InputTooLarge
            : issues.Any(issue => issue.Code == RubikStateErrorCode.MalformedJson) ? RubikFileErrorCode.MalformedDocument
            : RubikFileErrorCode.ValidationFailed;
        return Failure(code, path, string.Join("; ", issues.Select(issue => $"{issue.Path}: {issue.Message}")));
    }

    private static RubikFileOperationResult FromException(string path, Exception exception, bool replacing)
    {
        var code = exception switch
        {
            OperationCanceledException => RubikFileErrorCode.Cancelled,
            UnauthorizedAccessException => RubikFileErrorCode.AccessDenied,
            ArgumentException or NotSupportedException or PathTooLongException or DirectoryNotFoundException => RubikFileErrorCode.PathInvalid,
            IOException when (exception.HResult & 0xFFFF) is 0x27 or 0x70 => RubikFileErrorCode.DiskFull,
            IOException when replacing => RubikFileErrorCode.ReplaceFailed,
            IOException => RubikFileErrorCode.ReplaceFailed,
            InvalidOperationException => RubikFileErrorCode.ValidationFailed,
            _ => RubikFileErrorCode.InternalError
        };
        return Failure(code, path, exception.Message);
    }

    private static RubikFileOperationResult Failure(RubikFileErrorCode code, string path, string message) =>
        new(false, code, message, path);
}
