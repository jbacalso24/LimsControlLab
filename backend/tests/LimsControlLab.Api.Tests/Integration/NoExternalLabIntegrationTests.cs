#pragma warning disable CA1707

using System.Globalization;
using System.Reflection;
using LimsControlLab.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LimsControlLab.Api.Tests.Integration;

public sealed class NoExternalLabIntegrationTests
{
    [Fact]
    public void AssertNoExternalLabResultsEndpoint()
    {
        // R56: External laboratory analysis results and Factory Data SHALL NOT be captured in LIMS Control Lab.
        // This structural test verifies that no controller exists for capturing such data.

        var apiAssembly = typeof(Program).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal) &&
                   typeof(ControllerBase).IsAssignableFrom(t))
            .ToList();

        var externalLabControllers = controllerTypes
            .Where(ct => ContainsExternalLabKeywords(ct.Name))
            .ToList();

        // Assert: no controller for external lab results or Factory Data
        Assert.Empty(externalLabControllers);
    }

    [Fact]
    public void AssertCoreControllersExist()
    {
        // Verify that the core controllers (LIMS analyses, templates, etc.) are present.
        var apiAssembly = typeof(Program).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal) &&
                   typeof(ControllerBase).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        // Assert: at least the core controllers are present
        Assert.NotEmpty(controllerTypes);
        Assert.Contains("AnalysesController", controllerTypes);
    }

    private static bool ContainsExternalLabKeywords(string name)
    {
        var lowerName = name.ToLower(CultureInfo.InvariantCulture);
        var keywords = new[]
        {
            "external", "scada", "factory", "gateway", "externallabresult",
            "factorydata", "scadadata", "externalanalysis", "labresult"
        };

        return keywords.Any(kw => lowerName.Contains(kw, StringComparison.Ordinal));
    }
}
