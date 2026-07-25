using System.Text.Json;
using ModelAssets;

if (args.Length != 2 || args[0] != "--manifest")
{
    Console.Error.WriteLine("Usage: ModelAssetValidator --manifest <asset-manifest-v2.json>");
    return 2;
}

var report = new ModelAssetValidator().Validate(new() { ManifestPath = args[1] });
var output = new
{
    format = "chess-model-validation",
    version = "1.0",
    setId = report.SetId,
    isValid = report.IsValid,
    issues = report.Issues.Select(issue => new
    {
        severity = issue.Severity.ToString(),
        issue.Code,
        issue.Message,
        issue.AssetId
    }),
    stats = report.Stats
};
Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
return report.IsValid ? 0 : 1;
