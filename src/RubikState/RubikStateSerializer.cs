using System.Text;
using System.Text.Json;

namespace RubikState;

public static class RubikStateSerializer
{
    public const int DefaultMaximumBytes = 1024 * 1024;

    private static readonly HashSet<string> RootProperties = new(StringComparer.Ordinal)
    {
        "format", "version", "size", "faceOrder", "colorScheme", "faces",
        "stateHash", "source", "createdUtc", "metadata"
    };

    private static readonly string[] Faces = ["U", "R", "F", "D", "L", "B"];
    private static readonly Dictionary<string, int> ColorNames = new(StringComparer.Ordinal)
    {
        ["white"] = 1, ["red"] = 2, ["green"] = 3,
        ["yellow"] = 4, ["orange"] = 5, ["blue"] = 6
    };

    public static RubikStateReadResult Parse(ReadOnlySpan<byte> utf8, int maximumBytes = DefaultMaximumBytes)
    {
        if (utf8.Length > maximumBytes)
            return Fail(RubikStateErrorCode.InputTooLarge, "$", $"Input is {utf8.Length} bytes; limit is {maximumBytes}.");

        try
        {
            using var json = JsonDocument.Parse(utf8.ToArray(), new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32
            });

            var duplicate = FindDuplicate(json.RootElement, "$", inspectMetadata: true);
            if (duplicate is not null)
                return RubikStateReadResult.Failed(duplicate);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
                return Fail(RubikStateErrorCode.InvalidValue, "$", "Root must be an object.");

            foreach (var property in json.RootElement.EnumerateObject())
            {
                if (!RootProperties.Contains(property.Name))
                    return Fail(RubikStateErrorCode.UnknownProperty, $"$.{property.Name}", "Unknown root property.");
            }

            var format = RequiredString(json.RootElement, "format");
            var version = RequiredInt(json.RootElement, "version");
            var size = RequiredInt(json.RootElement, "size");
            var faceOrder = RequiredArray(json.RootElement, "faceOrder").EnumerateArray().Select(StringValue).ToArray();
            var schemeElement = RequiredObject(json.RootElement, "colorScheme");
            var facesElement = RequiredObject(json.RootElement, "faces");
            EnsureExactFaceProperties(schemeElement, "$.colorScheme");
            EnsureExactFaceProperties(facesElement, "$.faces");

            var scheme = new RubikColorScheme(
                RequiredString(schemeElement, "U"), RequiredString(schemeElement, "R"),
                RequiredString(schemeElement, "F"), RequiredString(schemeElement, "D"),
                RequiredString(schemeElement, "L"), RequiredString(schemeElement, "B"));
            var parsedFaces = new RubikFaces(
                ParseFace(facesElement, "U"), ParseFace(facesElement, "R"),
                ParseFace(facesElement, "F"), ParseFace(facesElement, "D"),
                ParseFace(facesElement, "L"), ParseFace(facesElement, "B"));

            var stateHash = RequiredString(json.RootElement, "stateHash");
            var source = RequiredString(json.RootElement, "source");
            var createdUtc = RequiredString(json.RootElement, "createdUtc");
            JsonElement? metadata = null;
            if (json.RootElement.TryGetProperty("metadata", out var metadataElement))
            {
                if (metadataElement.ValueKind != JsonValueKind.Object)
                    throw new DocumentException(RubikStateErrorCode.InvalidValue, "$.metadata", "metadata must be an object.");
                ValidateMetadata(metadataElement, "$.metadata");
                metadata = metadataElement.Clone();
            }

            var document = new RubikStateDocument(format, version, size, faceOrder, scheme, parsedFaces,
                stateHash, source, createdUtc, metadata);
            var validation = RubikStateValidator.Validate(document);
            if (!validation.IsValid)
                return new RubikStateReadResult(false, null, validation.Issues);

            var hash = RubikStateHasher.Calculate(document);
            var normalized = document with { StateHash = hash };
            return new RubikStateReadResult(true, new RubikStateLoadPlan(normalized, parsedFaces.Flatten(), hash), []);
        }
        catch (DocumentException exception)
        {
            return Fail(exception.Code, exception.Path, exception.Message);
        }
        catch (JsonException exception)
        {
            return Fail(RubikStateErrorCode.MalformedJson, "$", exception.Message);
        }
        catch (Exception exception) when (exception is InvalidOperationException or OverflowException)
        {
            return Fail(RubikStateErrorCode.InvalidValue, "$", exception.Message);
        }
    }

    public static RubikStateReadResult Parse(string json, int maximumBytes = DefaultMaximumBytes) =>
        Parse(Encoding.UTF8.GetBytes(json), maximumBytes);

    public static byte[] SerializeToUtf8(RubikStateDocument document)
    {
        var validation = RubikStateValidator.Validate(document, verifyHash: true);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Issues.Select(issue => $"{issue.Path}: {issue.Message}")));

        var normalized = document with { StateHash = RubikStateHasher.Calculate(document) };
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", normalized.Format);
            writer.WriteNumber("version", normalized.Version);
            writer.WriteNumber("size", normalized.Size);
            writer.WritePropertyName("faceOrder");
            writer.WriteStartArray();
            foreach (var face in normalized.FaceOrder) writer.WriteStringValue(face);
            writer.WriteEndArray();
            writer.WritePropertyName("colorScheme");
            writer.WriteStartObject();
            foreach (var item in normalized.ColorScheme.InFaceOrder()) writer.WriteString(item.Key, item.Value);
            writer.WriteEndObject();
            writer.WritePropertyName("faces");
            writer.WriteStartObject();
            foreach (var face in normalized.Faces.InFaceOrder())
            {
                writer.WritePropertyName(face.Key);
                writer.WriteStartArray();
                foreach (var value in face.Value) writer.WriteNumberValue(value);
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
            writer.WriteString("stateHash", normalized.StateHash);
            writer.WriteString("source", normalized.Source);
            writer.WriteString("createdUtc", normalized.CreatedUtc);
            if (normalized.Metadata is { } metadata)
            {
                writer.WritePropertyName("metadata");
                WriteCanonical(metadata, writer);
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static string Serialize(RubikStateDocument document) => Encoding.UTF8.GetString(SerializeToUtf8(document));

    private static int[] ParseFace(JsonElement faces, string name)
    {
        var array = RequiredArray(faces, name);
        var values = new List<int>();
        foreach (var token in array.EnumerateArray())
        {
            if (token.ValueKind == JsonValueKind.Number && token.TryGetInt32(out var number))
                values.Add(number);
            else if (token.ValueKind == JsonValueKind.String && ColorNames.TryGetValue(token.GetString() ?? string.Empty, out var color))
                values.Add(color);
            else
                throw new DocumentException(RubikStateErrorCode.InvalidValue, $"$.faces.{name}", "Face values must be color IDs 1..6 or canonical color names.");
        }
        return values.ToArray();
    }

    private static void EnsureExactFaceProperties(JsonElement element, string path)
    {
        var names = element.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        var missing = Faces.FirstOrDefault(face => !names.Contains(face));
        if (missing is not null)
            throw new DocumentException(RubikStateErrorCode.MissingProperty, $"{path}.{missing}", "Required face is missing.");
        var extra = names.FirstOrDefault(name => !Faces.Contains(name, StringComparer.Ordinal));
        if (extra is not null)
            throw new DocumentException(RubikStateErrorCode.UnknownProperty, $"{path}.{extra}", "Unknown face property.");
    }

    private static RubikStateIssue? FindDuplicate(JsonElement element, string path, bool inspectMetadata)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    return new(RubikStateErrorCode.DuplicateProperty, $"{path}.{property.Name}", "Duplicate JSON property.");
                if (inspectMetadata || !string.Equals(property.Name, "metadata", StringComparison.Ordinal))
                {
                    var nested = FindDuplicate(property.Value, $"{path}.{property.Name}", inspectMetadata);
                    if (nested is not null) return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindDuplicate(item, $"{path}[{index++}]", inspectMetadata);
                if (nested is not null) return nested;
            }
        }
        return null;
    }

    private static void ValidateMetadata(JsonElement element, string path)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("command", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("executable", StringComparison.OrdinalIgnoreCase))
                throw new DocumentException(RubikStateErrorCode.InvalidValue, $"{path}.{property.Name}", "Executable metadata is not allowed.");
            if (property.Value.ValueKind == JsonValueKind.String && Path.IsPathFullyQualified(property.Value.GetString() ?? string.Empty))
                throw new DocumentException(RubikStateErrorCode.InvalidValue, $"{path}.{property.Name}", "Absolute paths are not allowed.");
            if (property.Value.ValueKind == JsonValueKind.Object)
                ValidateMetadata(property.Value, $"{path}.{property.Name}");
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var value in property.Value.EnumerateArray())
                    if (value.ValueKind == JsonValueKind.String && Path.IsPathFullyQualified(value.GetString() ?? string.Empty))
                        throw new DocumentException(RubikStateErrorCode.InvalidValue, $"{path}.{property.Name}", "Absolute paths are not allowed.");
            }
        }
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonical(item, writer);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string StringValue(JsonElement value) => value.ValueKind == JsonValueKind.String
        ? value.GetString() ?? string.Empty
        : throw new DocumentException(RubikStateErrorCode.InvalidValue, "$", "Expected string value.");

    private static string RequiredString(JsonElement parent, string name) =>
        Required(parent, name).ValueKind == JsonValueKind.String
            ? Required(parent, name).GetString() ?? string.Empty
            : throw new DocumentException(RubikStateErrorCode.InvalidValue, $"$.{name}", "Expected string.");

    private static int RequiredInt(JsonElement parent, string name) =>
        Required(parent, name).ValueKind == JsonValueKind.Number && Required(parent, name).TryGetInt32(out var result)
            ? result
            : throw new DocumentException(RubikStateErrorCode.InvalidValue, $"$.{name}", "Expected integer.");

    private static JsonElement RequiredArray(JsonElement parent, string name)
    {
        var value = Required(parent, name);
        return value.ValueKind == JsonValueKind.Array ? value
            : throw new DocumentException(RubikStateErrorCode.InvalidValue, $"$.{name}", "Expected array.");
    }

    private static JsonElement RequiredObject(JsonElement parent, string name)
    {
        var value = Required(parent, name);
        return value.ValueKind == JsonValueKind.Object ? value
            : throw new DocumentException(RubikStateErrorCode.InvalidValue, $"$.{name}", "Expected object.");
    }

    private static JsonElement Required(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) ? value
            : throw new DocumentException(RubikStateErrorCode.MissingProperty, $"$.{name}", "Required property is missing.");

    private static RubikStateReadResult Fail(RubikStateErrorCode code, string path, string message) =>
        RubikStateReadResult.Failed(new RubikStateIssue(code, path, message));

    private sealed class DocumentException(RubikStateErrorCode code, string path, string message) : Exception(message)
    {
        public RubikStateErrorCode Code { get; } = code;
        public string Path { get; } = path;
    }
}
