using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ChessApp;

internal sealed record Chess3DNativeRuntimeInfo(
    string Platform,
    string OSDescription,
    string ProcessArchitecture,
    string ImportLibraryName,
    string ExpectedLibraryName,
    string ExpectedLibraryPath,
    bool ExpectedLibraryExists,
    bool IsSupportedPlatform,
    string LoadError);

internal static class Chess3DNativeLibraryResolver
{
    public const string ImportLibraryName = "Chess3DEngine.dll";
    public const string WindowsLibraryName = "Chess3DEngine.dll";
    public const string LinuxLibraryName = "libChess3DEngine.so";
    public const string MacOSLibraryName = "libChess3DEngine.dylib";

    private static readonly object Gate = new();
    private static readonly HashSet<Assembly> RegisteredAssemblies = new();
    private static string _lastLoadError = "";

    public static void EnsureRegistered(Assembly assembly)
    {
        lock (Gate)
        {
            if (RegisteredAssemblies.Contains(assembly))
            {
                return;
            }

            NativeLibrary.SetDllImportResolver(assembly, ResolveNativeLibrary);
            RegisteredAssemblies.Add(assembly);
        }
    }

    public static Chess3DNativeRuntimeInfo GetRuntimeInfo()
    {
        var expectedName = GetExpectedLibraryName();
        var expectedPath = Path.Combine(AppContext.BaseDirectory, expectedName);
        return new Chess3DNativeRuntimeInfo(
            GetPlatformName(),
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            ImportLibraryName,
            expectedName,
            expectedPath,
            File.Exists(expectedPath),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            _lastLoadError);
    }

    public static string GetExpectedLibraryNameForPlatform(string platformName)
    {
        return platformName.Equals("Linux", StringComparison.OrdinalIgnoreCase)
            ? LinuxLibraryName
            : platformName.Equals("OSX", StringComparison.OrdinalIgnoreCase) || platformName.Equals("macOS", StringComparison.OrdinalIgnoreCase)
                ? MacOSLibraryName
                : WindowsLibraryName;
    }

    public static string GetExpectedLibraryPathForPlatform(string platformName, string baseDirectory)
    {
        return Path.Combine(baseDirectory, GetExpectedLibraryNameForPlatform(platformName));
    }

    private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!IsChess3DLibraryName(libraryName))
        {
            return IntPtr.Zero;
        }

        var expectedName = GetExpectedLibraryName();
        var candidates = BuildCandidatePaths(expectedName);
        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out var handle))
            {
                _lastLoadError = "";
                return handle;
            }
        }

        if (NativeLibrary.TryLoad(expectedName, assembly, searchPath, out var fallbackHandle))
        {
            _lastLoadError = "";
            return fallbackHandle;
        }

        _lastLoadError = $"Could not resolve {libraryName}; expected {expectedName} in {AppContext.BaseDirectory}.";
        return IntPtr.Zero;
    }

    private static bool IsChess3DLibraryName(string libraryName)
    {
        var fileName = Path.GetFileName(libraryName);
        return string.Equals(fileName, ImportLibraryName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, WindowsLibraryName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, LinuxLibraryName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, MacOSLibraryName, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] BuildCandidatePaths(string expectedName)
    {
        return new[]
        {
            Path.Combine(AppContext.BaseDirectory, expectedName),
            Path.Combine(AppContext.BaseDirectory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native", expectedName)
        };
    }

    private static string GetExpectedLibraryName()
    {
        return GetExpectedLibraryNameForPlatform(GetPlatformName());
    }

    private static string GetPlatformName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "OSX";
        }

        return "Unknown";
    }
}
