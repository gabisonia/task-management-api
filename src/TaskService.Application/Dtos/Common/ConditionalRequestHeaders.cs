namespace TaskService.Application.Dtos.Common;

public sealed class ConditionalRequestHeaders
{
    public string? IfMatch { get; set; }
    public string? IfNoneMatch { get; set; }
}

public sealed class EntityTag
{
    public string Value { get; set; } = string.Empty;
    public bool IsWeak { get; set; }
    public string ToHeaderValue() => IsWeak ? $"W/\"{Value}\"" : $"\"{Value}\"";

    public static EntityTag Parse(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return new EntityTag { Value = string.Empty };
        }

        bool isWeak = headerValue.StartsWith("W/", StringComparison.OrdinalIgnoreCase);
        string value = headerValue
            .Replace("W/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim('"');

        return new EntityTag { Value = value, IsWeak = isWeak };
    }
}

