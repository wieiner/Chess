using System.Runtime.InteropServices;
using ChessApp;

namespace ChessOnlineProtocol;

public enum AuthorityRuntimeKind
{
    Unsupported = 0,
    WindowsNative = 1,
    LinuxNativeFuture = 2,
    ManagedStubForTests = 3
}

public sealed record OnlineAuthorityRuntimeDiagnostics(
    AuthorityRuntimeKind RuntimeKind,
    string RuntimeKindName,
    string Platform,
    string OSDescription,
    string ProcessArchitecture,
    string NativeLibraryName,
    string NativeLibraryPath,
    bool IsPortableRuntime,
    bool IsSupported);

public interface IChessOnlineRulesAuthority : IDisposable
{
    string RulesetId { get; }
    string StateHash { get; }
    int ActionCount { get; }
    string LastActionNotation { get; }
    int GamePhase { get; }
    int GameOutcome { get; }
    string TurnSummary { get; }

    OnlineActionCommand? FirstLegalNormalMoveCommand(int actorSide);
    OnlineActionCommand? FirstAiCandidateCommand(string preferredKind = "");
    OnlineLegalPreviewResult BuildLegalPreview(OnlineLegalPreviewRequest request, string roomId, string tableId, long serverSeq);
    bool TryApply(OnlineActionCommand command, out string rejectReason, out string rejectText);
    OnlineSnapshot CreateSnapshot(string roomId, string tableId, long serverSeq);
}

public interface IChessOnlineGameSessionFactory
{
    IChessOnlineRulesAuthority Create(RuleProfileInfo profile, string profileRoot);
    OnlineAuthorityRuntimeDiagnostics GetDiagnostics();
}

public sealed class NativeChessOnlineGameSessionFactory : IChessOnlineGameSessionFactory
{
    public IChessOnlineRulesAuthority Create(RuleProfileInfo profile, string profileRoot)
    {
        return new OnlineGameSession(profile, profileRoot);
    }

    public OnlineAuthorityRuntimeDiagnostics GetDiagnostics()
    {
        var native = NativeChess3DEngine.GetNativeRuntimeInfo();
        var runtimeKind = native.Platform switch
        {
            "Windows" => AuthorityRuntimeKind.WindowsNative,
            "Linux" => AuthorityRuntimeKind.LinuxNativeFuture,
            _ => AuthorityRuntimeKind.Unsupported
        };
        return new OnlineAuthorityRuntimeDiagnostics(
            runtimeKind,
            runtimeKind.ToString(),
            native.Platform,
            native.OSDescription,
            native.ProcessArchitecture,
            native.ExpectedLibraryName,
            native.ExpectedLibraryExists ? native.ExpectedLibraryPath : "",
            IsPortableRuntime: native.IsSupportedPlatform,
            IsSupported: native.IsSupportedPlatform && native.ExpectedLibraryExists);
    }

    public static string GetExpectedNativeLibraryNameForPlatform(string platformName)
    {
        return Chess3DNativeLibraryResolver.GetExpectedLibraryNameForPlatform(platformName);
    }

    public static string GetExpectedNativeLibraryPathForPlatform(string platformName, string baseDirectory)
    {
        return Chess3DNativeLibraryResolver.GetExpectedLibraryPathForPlatform(platformName, baseDirectory);
    }

    public static string HashFromSaveGameJson(string saveGameJson)
    {
        using var engine = new NativeChess3DEngine();
        if (!engine.LoadSaveGameJson(saveGameJson))
        {
            throw new InvalidOperationException("Snapshot savegame did not load into a fresh engine.");
        }
        return engine.GetStateHash();
    }
}
