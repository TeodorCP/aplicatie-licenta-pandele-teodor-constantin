using System.Text.Json;
using BusinessOps.Backend.DTOs;

namespace BusinessOps.Backend.Helpers;

public static class ValidationHelpers
{
    public static string? ValidateModuleRequest(CreateModuleRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required.";
        }

        if (request.Fields.Count == 0)
        {
            return "At least one field is required.";
        }

        var duplicateCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in request.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name))
            {
                return "Each field must have a name.";
            }

            if (!duplicateCheck.Add(field.Name.Trim()))
            {
                return $"Duplicate field name: {field.Name}";
            }

            if (!FieldTypeConstants.Allowed.Contains(field.Type))
            {
                return $"Invalid field type '{field.Type}'. Allowed: number, text, boolean, select, timestamp.";
            }
        }

        return null;
    }

    public static string? ValidateEntryData(JsonElement data, Dictionary<string, string> fieldDefinitions)
    {
        if (fieldDefinitions.Count == 0)
        {
            return "Module has no valid fields configured.";
        }

        if (data.ValueKind != JsonValueKind.Object)
        {
            return "Entry data must be a JSON object.";
        }

        foreach (var jsonProperty in data.EnumerateObject())
        {
            if (!fieldDefinitions.TryGetValue(jsonProperty.Name, out var expectedType))
            {
                return $"Unknown field '{jsonProperty.Name}' for this module.";
            }

            if (!IsValueTypeValid(jsonProperty.Value, FieldTypeConstants.NormalizeFieldType(expectedType)))
            {
                return $"Field '{jsonProperty.Name}' must be of type '{FieldTypeConstants.NormalizeFieldType(expectedType)}'.";
            }
        }

        return null;
    }

    private static bool IsValueTypeValid(JsonElement value, string expectedType)
    {
        return expectedType.ToLowerInvariant() switch
        {
            "number" => value.ValueKind == JsonValueKind.Number,
            "text" => value.ValueKind == JsonValueKind.String,
            "boolean" => value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False,
            "select" => value.ValueKind == JsonValueKind.String,
            "timestamp" => ValidateTimestamp(value),
            _ => false
        };
    }

    private static bool ValidateTimestamp(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var raw = value.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return DateTime.TryParse(
            raw,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out _);
    }
}
