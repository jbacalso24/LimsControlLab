#pragma warning disable CA1707

using LimsControlLab.Domain.Calculations;
using LimsControlLab.Domain.Entities;
using Xunit;

namespace LimsControlLab.Api.Tests.Calculations;

public sealed class CalculationEngineTests
{
    [Fact]
    public void InterpolateCalibrationValue_WithExactMatch_ReturnsYValue()
    {
        var points = new[]
        {
            new CalibrationPoint { Id = 1, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { Id = 2, XValue = 10m, YValue = 100m, Order = 1 },
            new CalibrationPoint { Id = 3, XValue = 20m, YValue = 200m, Order = 2 },
        };

        var result = CalculationEngine.InterpolateCalibrationValue(10m, points);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void InterpolateCalibrationValue_BetweenPoints_InterpolatesLinearlyDown()
    {
        var points = new[]
        {
            new CalibrationPoint { Id = 1, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { Id = 2, XValue = 10m, YValue = 100m, Order = 1 },
            new CalibrationPoint { Id = 3, XValue = 20m, YValue = 200m, Order = 2 },
        };

        var result = CalculationEngine.InterpolateCalibrationValue(5m, points);

        // Linear interpolation: y = 0 + (5 - 0) * (100 - 0) / (10 - 0) = 50
        Assert.Equal(50m, result);
    }

    [Fact]
    public void InterpolateCalibrationValue_BetweenPoints_InterpolatesLinearlyUp()
    {
        var points = new[]
        {
            new CalibrationPoint { Id = 1, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { Id = 2, XValue = 10m, YValue = 100m, Order = 1 },
            new CalibrationPoint { Id = 3, XValue = 20m, YValue = 200m, Order = 2 },
        };

        var result = CalculationEngine.InterpolateCalibrationValue(15m, points);

        // Linear interpolation: y = 100 + (15 - 10) * (200 - 100) / (20 - 10) = 150
        Assert.Equal(150m, result);
    }

    [Fact]
    public void InterpolateCalibrationValue_BelowMinimum_ReturnsNull()
    {
        var points = new[]
        {
            new CalibrationPoint { Id = 1, XValue = 10m, YValue = 100m, Order = 0 },
            new CalibrationPoint { Id = 2, XValue = 20m, YValue = 200m, Order = 1 },
        };

        var result = CalculationEngine.InterpolateCalibrationValue(5m, points);

        Assert.Null(result);
    }

    [Fact]
    public void InterpolateCalibrationValue_AboveMaximum_ReturnsNull()
    {
        var points = new[]
        {
            new CalibrationPoint { Id = 1, XValue = 10m, YValue = 100m, Order = 0 },
            new CalibrationPoint { Id = 2, XValue = 20m, YValue = 200m, Order = 1 },
        };

        var result = CalculationEngine.InterpolateCalibrationValue(25m, points);

        Assert.Null(result);
    }

    [Fact]
    public void InterpolateCalibrationValue_EmptyPoints_ReturnsNull()
    {
        var points = Array.Empty<CalibrationPoint>();

        var result = CalculationEngine.InterpolateCalibrationValue(10m, points);

        Assert.Null(result);
    }

    [Fact]
    public void InterpolateCalibrationValue_SinglePoint_ReturnsYValue()
    {
        var points = new[]
        {
            new CalibrationPoint { Id = 1, XValue = 10m, YValue = 100m, Order = 0 },
        };

        var result = CalculationEngine.InterpolateCalibrationValue(10m, points);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void InterpolateCalibrationValue_SinglePoint_WrongX_ReturnsNull()
    {
        var points = new[]
        {
            new CalibrationPoint { Id = 1, XValue = 10m, YValue = 100m, Order = 0 },
        };

        var result = CalculationEngine.InterpolateCalibrationValue(5m, points);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateSimpleAverage_MultipleValues_ReturnsAverage()
    {
        var values = new[] { 10m, 20m, 30m };

        var result = CalculationEngine.CalculateSimpleAverage(values);

        Assert.Equal(20m, result);
    }

    [Fact]
    public void CalculateSimpleAverage_SingleValue_ReturnsThatValue()
    {
        var values = new[] { 15m };

        var result = CalculationEngine.CalculateSimpleAverage(values);

        Assert.Equal(15m, result);
    }

    [Fact]
    public void CalculateSimpleAverage_EmptyCollection_ReturnsNull()
    {
        var values = Array.Empty<decimal>();

        var result = CalculationEngine.CalculateSimpleAverage(values);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateWeightedAverage_DistributedWeights_ReturnsWeightedAverage()
    {
        var pairs = new[]
        {
            (10m, 0.5m),
            (20m, 0.5m),
        };

        var result = CalculationEngine.CalculateWeightedAverage(pairs);

        Assert.Equal(15m, result);
    }

    [Fact]
    public void CalculateWeightedAverage_UnevenWeights_ReturnsWeightedAverage()
    {
        var pairs = new[]
        {
            (10m, 0.25m),
            (20m, 0.75m),
        };

        var result = CalculationEngine.CalculateWeightedAverage(pairs);

        Assert.Equal(17.5m, result);
    }

    [Fact]
    public void CalculateWeightedAverage_EmptyCollection_ReturnsNull()
    {
        var pairs = Array.Empty<(decimal, decimal)>();

        var result = CalculationEngine.CalculateWeightedAverage(pairs);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateWeightedAverage_ZeroTotalWeight_ReturnsNull()
    {
        var pairs = new[]
        {
            (10m, 0m),
            (20m, 0m),
        };

        var result = CalculationEngine.CalculateWeightedAverage(pairs);

        Assert.Null(result);
    }
}
