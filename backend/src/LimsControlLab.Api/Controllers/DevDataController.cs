using LimsControlLab.Api.Auth;
using LimsControlLab.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimsControlLab.Api.Controllers;

/// <summary>
/// Data-management endpoints for the illustrative demo (reset and reseed).
/// Enabled in all environments because this is a demo deployment; any authenticated
/// user can reset the shared demo dataset back to its seeded state.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize]
public sealed class DevDataController : ControllerBase
{
    private readonly LimsDbContext _db;
    private readonly PasswordHasher _hasher;

    public DevDataController(LimsDbContext db, PasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>
    /// Reset and reseed the database with the illustrative demo dataset.
    /// </summary>
    [HttpPost("reset-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetData(CancellationToken ct)
    {
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
