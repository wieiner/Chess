using System.Text.Json;

namespace RubikState;

public enum RubikValidationSeverity { Info, Warning, Error }

public static class RubikValidationCodes
{
    public const string WrongFaceCount = "wrongFaceCount";
    public const string WrongFaceSize = "wrongFaceSize";
    public const string UnknownColor = "unknownColor";
    public const string ColorUnderflow = "colorUnderflow";
    public const string ColorOverflow = "colorOverflow";
    public const string MissingSticker = "missingSticker";
    public const string DuplicateSticker = "duplicateSticker";
    public const string InvalidCenterScheme = "invalidCenterScheme";
    public const string HashMismatch = "hashMismatch";
    public const string UnsupportedVersion = "unsupportedVersion";
}

public sealed record RubikValidationIssue(
    RubikValidationSeverity Severity,
    string Code,
    string? Face,
    int? Row,
    int? Column,
    string? CubieClass,
    string Message,
    string SuggestedAction)
{
    public string DisplayText => $"[{Severity}] {Code}" +
        (Face is null ? string.Empty : $" {Face}[{(Row ?? 0) + 1},{(Column ?? 0) + 1}]") +
        $": {Message}";
}

public sealed record RubikValidationDiagnosticReport(
    int Size,
    int ErrorCount,
    int WarningCount,
    bool BasicCountsValid,
    IReadOnlyList<RubikValidationIssue> Issues)
{
    public string ToSanitizedJson() => JsonSerializer.Serialize(new
    {
        format = "rubik.validation-report",
        version = 1,
        size = Size,
        errorCount = ErrorCount,
        warningCount = WarningCount,
        basicCountsValid = BasicCountsValid,
        issues = Issues.Select(issue => new
        {
            severity = issue.Severity.ToString().ToLowerInvariant(), issue.Code, issue.Face,
            issue.Row, issue.Column, issue.CubieClass, issue.Message, issue.SuggestedAction
        })
    }, new JsonSerializerOptions { WriteIndented = true });
}

public static class RubikPhysicalStateDiagnostics
{
    public static RubikValidationDiagnosticReport ValidateDraft(RubikFaceEditorDraft draft, int maximumCellIssues = 128)
    {
        var issues = new List<RubikValidationIssue>();
        var names = new[] { "U", "R", "F", "D", "L", "B" };
        var emittedCells = 0;
        for (var face = 0; face < 6; face++)
        for (var row = 0; row < draft.Size; row++)
        for (var column = 0; column < draft.Size; column++)
        {
            var value = draft.GetCell(face, row, column);
            if (value == 0 && emittedCells++ < maximumCellIssues)
                issues.Add(new(RubikValidationSeverity.Error, RubikValidationCodes.MissingSticker, names[face], row, column,
                    null, "Sticker has no color.", "Select a palette color and paint this cell."));
            else if (value is < 0 or > 6 && emittedCells++ < maximumCellIssues)
                issues.Add(new(RubikValidationSeverity.Error, RubikValidationCodes.UnknownColor, names[face], row, column,
                    null, $"Color ID {value} is not recognized.", "Replace it with a color ID from 1 through 6."));
        }

        var summary = draft.Summarize();
        var expected = draft.Size * draft.Size;
        foreach (var color in Enumerable.Range(1, 6))
        {
            var actual = summary.ColorCounts[color];
            if (actual < expected)
                issues.Add(new(RubikValidationSeverity.Error, RubikValidationCodes.ColorUnderflow, null, null, null,
                    null, $"Color {color} has {actual} stickers; expected {expected}.", $"Add {expected - actual} sticker(s) of color {color}."));
            if (actual > expected)
                issues.Add(new(RubikValidationSeverity.Error, RubikValidationCodes.ColorOverflow, null, null, null,
                    null, $"Color {color} has {actual} stickers; expected {expected}.", $"Replace {actual - expected} excess sticker(s)."));
        }

        if (draft.Size % 2 == 1)
        {
            var middle = draft.Size / 2;
            var centers = Enumerable.Range(0, 6).Select(face => draft.GetCell(face, middle, middle)).ToArray();
            if (centers.All(color => color != 0) && centers.Distinct().Count() != 6)
                issues.Add(new(RubikValidationSeverity.Error, RubikValidationCodes.InvalidCenterScheme, null, null, null,
                    "center", "Odd-N face centers are not six distinct colors.", "Check face labels and center stickers; no automatic reorientation was applied."));
        }

        var errors = issues.Count(issue => issue.Severity == RubikValidationSeverity.Error);
        var warnings = issues.Count(issue => issue.Severity == RubikValidationSeverity.Warning);
        return new RubikValidationDiagnosticReport(draft.Size, errors, warnings, summary.BasicCountsValid && errors == 0, issues);
    }
}
