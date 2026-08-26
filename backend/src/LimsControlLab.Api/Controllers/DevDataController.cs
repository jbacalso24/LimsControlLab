using LimsControlLab.Api.Auth;
using LimsControlLab.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

/// <summary>
/// Development-only endpoints for data management (reset and reseed).
/// Only available in Development environment.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize]
public sealed class DevDataController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly LimsDbContext _db;
    private readonly PasswordHasher _hasher;

    public DevDataController(IHostEnvironment environment, LimsDbContext db, PasswordHasher hasher)
    {
        _environment = environment;
        _db = db;
        _hasher = hasher;
    }

    /// <summary>
    /// Reset and reseed the database with illustrative development data.
    /// Only available in Development environment.
    /// </summary>
    [HttpPost("reset-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetData(CancellationToken ct)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        await SeedData.ResetAndReseedAsync(
            _db,
            pwd => _hasher.HashPassword(null, pwd),
            ct);

        var stats = new
        {
            message = "Data reset and reseeded.",
            users = _db.Users.Count(),
            instruments = _db.Instruments.Count(),
            samplingMethods = _db.SamplingMethods.Count(),
            analysisTemplates = _db.AnalysisTemplates.Count(),
            schedules = _db.Schedules.Count(),
            samples = _db.Samples.Count(),
            analyses = _db.Analyses.Count(),
            readings = _db.Readings.Count(),
            exceptionRecords = _db.ExceptionRecords.Count(),
            calibrationCurves = _db.CalibrationCurves.Count(),
            sampleTransfers = _db.SampleTransfers.Count(),
            integrationLogs = _db.IntegrationLogs.Count(),
        };

        return Ok(stats);
    }
}
