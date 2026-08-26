using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Domain.Calculations;

/// <summary>
/// DI-free, pure-math calculation engine for calibration lookups and derived values.
/// No DbContext, no ServiceProvider, no injected dependencies — only plain data in, plain data out.
/// Unit-testable directly without mocks or test infrastructure.
/// </summary>
public sealed class CalculationEngine
{
    /// <summary>
    /// Interpolate a y-value from a calibration curve for a given x-input.
    /// Performs linear interpolation between the two nearest points; returns null if input is out of range.
    /// </summary>
    /// <param name="xInput">The input value to look up.</param>
    /// <param name="points">Calibration points sorted by XValue.</param>
    /// <returns>The interpolated y-value, or null if out of range.</returns>
    public static decimal? InterpolateCalibrationValue(decimal xInput, IEnumerable<CalibrationPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var sortedPoints = points.OrderBy(p => p.XValue).ToList();
        if (sortedPoints.Count == 0)
            return null;

        // Out of range: below minimum.
        if (xInput < sortedPoints[0].XValue)
            return null;

        // Out of range: above maximum.
        if (xInput > sortedPoints[^1].XValue)
            return null;

        // Exact match.
        var exact = sortedPoints.FirstOrDefault(p => p.XValue == xInput);
        if (exact != null)
            return exact.YValue;

        // Need at least 2 points for interpolation.
        if (sortedPoints.Count < 2)
            return null;

        // Linear interpolation between two nearest points.
        for (int i = 0; i < sortedPoints.Count - 1; i++)
        {
            var lower = sortedPoints[i];
            var upper = sortedPoints[i + 1];

            if (xInput >= lower.XValue && xInput <= upper.XValue)
            {
                var x1 = lower.XValue;
                var y1 = lower.YValue;
                var x2 = upper.XValue;
                var y2 = upper.YValue;

                // y = y1 + (x - x1) * (y2 - y1) / (x2 - x1)
                var slope = (y2 - y1) / (x2 - x1);
                var yInterpolated = y1 + (xInput - x1) * slope;
                return yInterpolated;
            }
        }

        return null;
    }

    /// <summary>
    /// Calculate a simple average from a collection of readings.
    /// </summary>
    /// <param name="values">The reading values to average.</param>
    /// <returns>The average, or null if the collection is empty.</returns>
    public static decimal? CalculateSimpleAverage(IEnumerable<decimal> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var list = values.ToList();
        return list.Count == 0 ? null : list.Average();
    }

    /// <summary>
    /// Calculate a weighted average from readings with associated weights.
    /// </summary>
    /// <param name="valueWeightPairs">Tuples of (value, weight). Weights should sum to 1 or 100 depending on the domain.</param>
    /// <returns>The weighted average, or null if the collection is empty.</returns>
    public static decimal? CalculateWeightedAverage(IEnumerable<(decimal Value, decimal Weight)> valueWeightPairs)
    {
        ArgumentNullException.ThrowIfNull(valueWeightPairs);
        var list = valueWeightPairs.ToList();
        if (list.Count == 0)
            return null;

        var totalWeight = list.Sum(p => p.Weight);
        if (totalWeight == 0)
            return null;

        var weightedSum = list.Sum(p => p.Value * p.Weight);
        return weightedSum / totalWeight;
    }
}
