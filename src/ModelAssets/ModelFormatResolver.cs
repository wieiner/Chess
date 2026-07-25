namespace ModelAssets;

public enum ModelFormatSelection
{
    Glb,
    Obj,
    Procedural
}

public enum ModelFallbackReason
{
    None,
    GlbMissing,
    GlbInvalid,
    UnsupportedFeature,
    ObjFallback,
    ProceduralFallback
}

public sealed record ModelFormatCandidate(
    string Format,
    string Path,
    bool Exists,
    bool IsValid,
    string? Failure = null);

public sealed record ModelFormatResolution(
    ModelFormatSelection Selection,
    string? Path,
    ModelFallbackReason Reason,
    string Diagnostics);

public static class ModelFormatResolver
{
    public static ModelFormatResolution Resolve(IEnumerable<ModelFormatCandidate> candidates)
    {
        var values = candidates.ToArray();
        var glb = values.FirstOrDefault(item => item.Format.Equals("glb", StringComparison.OrdinalIgnoreCase));
        if (glb is { Exists: true, IsValid: true })
            return new(ModelFormatSelection.Glb, glb.Path, ModelFallbackReason.None, "validated GLB");

        var glbReason = glb switch
        {
            null or { Exists: false } => ModelFallbackReason.GlbMissing,
            { Failure: { } failure } when failure.Contains("unsupported", StringComparison.OrdinalIgnoreCase) =>
                ModelFallbackReason.UnsupportedFeature,
            _ => ModelFallbackReason.GlbInvalid
        };
        var obj = values.FirstOrDefault(item => item.Format.Equals("obj", StringComparison.OrdinalIgnoreCase));
        if (obj is { Exists: true, IsValid: true })
            return new(ModelFormatSelection.Obj, obj.Path, ModelFallbackReason.ObjFallback,
                $"{glbReason}; validated OBJ fallback");
        return new(ModelFormatSelection.Procedural, null, ModelFallbackReason.ProceduralFallback,
            $"{glbReason}; OBJ unavailable or invalid; procedural fallback");
    }
}
