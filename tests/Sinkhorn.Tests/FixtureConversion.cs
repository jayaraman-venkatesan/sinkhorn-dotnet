namespace Sinkhorn.Tests;

using System.Text.Json;

internal static class FixtureConversion
{
    internal static double[,] ToRectangular(double[][] values)
    {
        var result = new double[values.Length, values[0].Length];
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values[i].Length; j++)
            {
                result[i, j] = values[i][j];
            }
        }

        return result;
    }

    internal static double DecodeDouble(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble();
        }

        return value.GetProperty("nonFinite").GetString() switch
        {
            "NaN" => double.NaN,
            "PositiveInfinity" => double.PositiveInfinity,
            "NegativeInfinity" => double.NegativeInfinity,
            string tag => throw new InvalidOperationException($"Unknown nonfinite tag '{tag}'."),
            null => throw new InvalidOperationException("The nonfinite tag was null."),
        };
    }
}
