namespace BusinessOps.Backend.Helpers;

public static class FieldTypeConstants
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "number",
        "text",
        "boolean",
        "select",
        "timestamp",
        "date",
        "time",
        "datetime"
    };

    public static bool IsTimestampField(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        var normalized = NormalizeFieldType(type);
        return string.Equals(normalized, "timestamp", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeFieldType(string? type)
    {
        var raw = type?.Trim().ToLowerInvariant() ?? string.Empty;
        return raw switch
        {
            "date" => "timestamp",
            "time" => "timestamp",
            "datetime" => "timestamp",
            _ => raw
        };
    }
}
