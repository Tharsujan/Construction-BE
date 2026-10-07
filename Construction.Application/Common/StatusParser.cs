namespace Construction.Application.Common;

public static class StatusParser
{
    public static bool TryParse<TEnum>(string? value, out TEnum? status, out string? error) where TEnum : struct, Enum
    {
        status = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse<TEnum>(value, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            error = $"Status '{value}' is not valid.";
            return false;
        }

        status = parsed;
        return true;
    }
}
