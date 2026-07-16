using System.Globalization;

namespace RubikState;

public enum RubikStateErrorCode
{
    None,
    InputTooLarge,
    MalformedJson,
    DuplicateProperty,
    UnknownProperty,
    MissingProperty,
    InvalidValue,
    UnsupportedVersion,
    UnsupportedSize,
    WrongFaceSize,
    WrongColorCount,
    HashMismatch
}

public sealed record RubikStateIssue(RubikStateErrorCode Code, string Path, string Message);

public sealed class RubikStateValidationResult
{
    public RubikStateValidationResult(IReadOnlyList<RubikStateIssue> issues) => Issues = issues;

    public IReadOnlyList<RubikStateIssue> Issues { get; }
    public bool IsValid => Issues.Count == 0;
}

public sealed record RubikStateReadResult(
    bool Success,
    RubikStateLoadPlan? Plan,
    IReadOnlyList<RubikStateIssue> Issues)
{
    public static RubikStateReadResult Failed(params RubikStateIssue[] issues) => new(false, null, issues);
}

public static class RubikStateValidator
{
    private static readonly Dictionary<string, string> ExpectedScheme = new(StringComparer.Ordinal)
    {
        ["U"] = "white", ["R"] = "red", ["F"] = "green",
        ["D"] = "yellow", ["L"] = "orange", ["B"] = "blue"
    };

    public static RubikStateValidationResult Validate(RubikStateDocument document, bool verifyHash = true)
    {
        var issues = new List<RubikStateIssue>();

        if (!string.Equals(document.Format, RubikStateDocument.CurrentFormat, StringComparison.Ordinal))
            Add(RubikStateErrorCode.InvalidValue, "$.format", $"Expected '{RubikStateDocument.CurrentFormat}'.");
        if (document.Version != RubikStateDocument.CurrentVersion)
            Add(RubikStateErrorCode.UnsupportedVersion, "$.version", $"Unsupported major version {document.Version}.");
        if (document.Size is < RubikStateDocument.MinimumSize or > RubikStateDocument.MaximumSize)
            Add(RubikStateErrorCode.UnsupportedSize, "$.size", "Supported cube sizes are 2 through 32.");

        if (!document.FaceOrder.SequenceEqual(RubikStateDocument.CanonicalFaceOrder, StringComparer.Ordinal))
            Add(RubikStateErrorCode.InvalidValue, "$.faceOrder", "Face order must be U,R,F,D,L,B.");

        foreach (var entry in document.ColorScheme.InFaceOrder())
        {
            if (!string.Equals(entry.Value, ExpectedScheme[entry.Key], StringComparison.Ordinal))
                Add(RubikStateErrorCode.InvalidValue, $"$.colorScheme.{entry.Key}", $"Expected '{ExpectedScheme[entry.Key]}'.");
        }

        if (document.Size is >= RubikStateDocument.MinimumSize and <= RubikStateDocument.MaximumSize)
        {
            var expected = checked(document.Size * document.Size);
            var counts = new int[7];
            foreach (var face in document.Faces.InFaceOrder())
            {
                if (face.Value.Length != expected)
                    Add(RubikStateErrorCode.WrongFaceSize, $"$.faces.{face.Key}", $"Expected {expected} values, found {face.Value.Length}.");
                foreach (var value in face.Value)
                {
                    if (value is < 1 or > 6)
                        Add(RubikStateErrorCode.InvalidValue, $"$.faces.{face.Key}", $"Color ID {value} is outside 1..6.");
                    else
                        counts[value]++;
                }
            }

            for (var color = 1; color <= 6; color++)
            {
                if (counts[color] != expected)
                    Add(RubikStateErrorCode.WrongColorCount, "$.faces", $"Color {color} occurs {counts[color]} times; expected {expected}.");
            }
        }

        if (Path.IsPathFullyQualified(document.Source))
            Add(RubikStateErrorCode.InvalidValue, "$.source", "Source must not contain an absolute path.");
        if (document.Source.Length > 128)
            Add(RubikStateErrorCode.InvalidValue, "$.source", "Source is limited to 128 characters.");
        if (document.CreatedUtc.Length > 0 &&
            !DateTimeOffset.TryParse(document.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            Add(RubikStateErrorCode.InvalidValue, "$.createdUtc", "createdUtc must be empty or an ISO-8601 timestamp.");

        if (!string.IsNullOrEmpty(document.StateHash))
        {
            if (document.StateHash.Length != 64 || document.StateHash.Any(c => !Uri.IsHexDigit(c)) ||
                !string.Equals(document.StateHash, document.StateHash.ToLowerInvariant(), StringComparison.Ordinal))
            {
                Add(RubikStateErrorCode.InvalidValue, "$.stateHash", "stateHash must be empty or 64 lowercase hexadecimal characters.");
            }
            else if (verifyHash && !string.Equals(document.StateHash, RubikStateHasher.Calculate(document), StringComparison.Ordinal))
            {
                Add(RubikStateErrorCode.HashMismatch, "$.stateHash", "Supplied state hash does not match normalized facelets.");
            }
        }

        return new RubikStateValidationResult(issues);

        void Add(RubikStateErrorCode code, string path, string message) => issues.Add(new(code, path, message));
    }
}
