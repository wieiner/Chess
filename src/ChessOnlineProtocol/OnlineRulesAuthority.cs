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
    private const string NativeLibraryName = "Chess3DEngine.dll";

    public IChessOnlineRulesAuthority Create(RuleProfileInfo profile, string profileRoot)
    {
        return new OnlineGameSession(profile, profileRoot);
    }

    public OnlineAuthorityRuntimeDiagnostics GetDiagnostics()
    {
        var nativePath = Path.Combine(AppContext.BaseDirectory, NativeLibraryName);
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var platform = isWindows
            ? "Windows"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? "Linux"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? "OSX"
                    : "Unknown";
        var runtimeKind = isWindows ? AuthorityRuntimeKind.WindowsNative : AuthorityRuntimeKind.Unsupported;
        return new OnlineAuthorityRuntimeDiagnostics(
            runtimeKind,
            runtimeKind.ToString(),
            platform,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            NativeLibraryName,
            File.Exists(nativePath) ? nativePath : "",
            IsPortableRuntime: false,
            IsSupported: isWindows && File.Exists(nativePath));
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
