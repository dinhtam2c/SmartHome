using System.Globalization;
using System.Text.Json;

namespace Application.Common.Capabilities;

internal static class CapabilityStateReader
{
    public static double? TryReadNumber(IReadOnlyDictionary<string, object?> state)
    {
        if (state.TryGetValue("value", out var value))
            return TryConvertToDouble(value);

        if (state.Count == 1)
            return TryConvertToDouble(state.Values.FirstOrDefault());

        return null;
    }

    private static double? TryConvertToDouble(object? value)
    {
        return value switch
        {
            null => null,
            JsonElement json when json.ValueKind == JsonValueKind.Number
                                  && json.TryGetDouble(out var number) => number,
            JsonElement json when json.ValueKind == JsonValueKind.String
                                  && double.TryParse(
                                      json.GetString(),
                                      NumberStyles.Float,
                                      CultureInfo.InvariantCulture,
                                      out var number) => number,
            byte number => number,
            sbyte number => number,
            short number => number,
            ushort number => number,
            int number => number,
            uint number => number,
            long number => number,
            ulong number => number,
            float number => number,
            double number => number,
            decimal number => (double)number,
            string text when double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var number) => number,
            _ => null
        };
    }
}
