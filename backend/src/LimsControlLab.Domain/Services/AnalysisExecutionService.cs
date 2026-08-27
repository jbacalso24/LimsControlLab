using System.Text.Json;
using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Calculations;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

public sealed class AnalysisExecutionService
{
    private readonly IAnalysisRepository _repository;
    private readonly ICalibrationCurveRepository _calibrationRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IUserRepository _userRepository;

    public AnalysisExecutionService(
        IAnalysisRepository repository,
        ICalibrationCurveRepository calibrationRepository,
        IAuditLogger auditLogger,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IUserRepository userRepository)
    {
        _repository = repository;
        _calibrationRepository = calibrationRepository;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _userRepository = userRepository;
    }

    public async Task<Outcome<ReadingCaptureResult>> CaptureReadingAsync(int analysisId, CaptureReadingRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var analysis = await _repository.GetByIdAsync(analysisId, ct);
        if (analysis == null)
            return new Outcome<ReadingCaptureResult>.NotFound($"Analysis {analysisId} not found.");

        var templateVersion = analysis.TemplateVersion;
        if (templateVersion == null)
            return new Outcome<ReadingCaptureResult>.NotFound($"Template version not found for analysis.");

        // Tests (and their units) are defined by the bound template version (R1).
        // When the template defines tests, the reading must target one of them and the
        // unit is taken from that test definition - the client cannot invent either.
        var definedTests = ParseTests(templateVersion.TestConfiguration);
        var unit = request.Unit;
        if (definedTests.Count > 0)
        {
            var matchedTest = definedTests.Find(t => t.Id == request.TestId);
            if (matchedTest == null)
                return new Outcome<ReadingCaptureResult>.Invalid(
                    "testId",
                    $"Test {request.TestId} is not defined for this analysis template.");
            unit = matchedTest.Unit;
        }

        var reading = new Reading
        {
            AnalysisId = analysisId,
            TestId = request.TestId,
            Value = request.Value,
            Unit = unit,
            CapturedAtUtc = request.CapturedAtUtc,
            CapturedByUserId = _currentUser.UserId,
            InstrumentId = request.InstrumentId,
            ValidationResult = "Valid",
        };

        var validationResult = ValidateReading(request.Value, templateVersion);
        reading.ValidationResult = validationResult.Status;

        await _repository.AddReadingAsync(reading, ct);

        if (validationResult.IsOutOfTolerance)
        {
            var exception = new ExceptionRecord
            {
                AnalysisId = analysisId,
                ReadingId = reading.Id,
                Reason = validationResult.Reason!,
            };
            await _repository.AddExceptionAsync(exception, ct);

            await _auditLogger.LogAsync(new AuditLogEntryRecord
            {
                UserId = _currentUser.UserId,
                Role = _currentUser.Role.ToString(),
                TimestampUtc = _timeProvider.GetUtcNow(),
                Action = "ExceptionCreated",
                EntityType = "ExceptionRecord",
                EntityId = exception.Id,
                AfterValues = $"Reason: {exception.Reason}",
            }, ct);
        }

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = _timeProvider.GetUtcNow(),
            Action = "ReadingCaptured",
            EntityType = "Reading",
            EntityId = reading.Id,
            AfterValues = $"Value: {reading.Value}, Status: {reading.ValidationResult}",
        }, ct);

        // Recompute derived values if the analysis is unlocked (R39, charter §2).
        // If locked, derived values freeze and do not change until a Lab Coordinator unlocks (R57).
        if (!analysis.IsLocked)
        {
            var curve = await _calibrationRepository.GetByAnalysisTemplateIdAsync(analysis.TemplateId, ct);
            if (curve != null)
            {
                var calibratedValue = CalculationEngine.InterpolateCalibrationValue(reading.Value, curve.Points);
                reading.CalibratedValue = calibratedValue;
            }
        }

        var validationDetail = new ReadingValidationDetail
        {
            IsValid = validationResult.Status == "Valid",
            ExpectedRange = validationResult.ExpectedRange,
            ActualValue = validationResult.ActualValue!,
            Reason = validationResult.Reason,
        };

        var result = new ReadingCaptureResult
        {
            Id = reading.Id,
            TestId = reading.TestId,
            Value = reading.Value,
            Unit = reading.Unit,
            CapturedAtUtc = reading.CapturedAtUtc,
            CapturedByUserId = _currentUser.UserId,
            CapturedByUsername = _currentUser.Username,
            ValidationResult = validationDetail,
            CalibratedValue = reading.CalibratedValue,
        };

        return new Outcome<ReadingCaptureResult>.Ok(result);
    }

    public async Task<Outcome<ExceptionDecisionResult>> DecideExceptionAsync(int analysisId, int exceptionId, ExceptionDecisionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.Decision))
            return new Outcome<ExceptionDecisionResult>.Invalid("decision", "Decision is required.");

        if (string.IsNullOrEmpty(request.Comment))
            return new Outcome<ExceptionDecisionResult>.Invalid("comment", "Comment is mandatory when deciding an exception (R36).");

        if (string.IsNullOrEmpty(request.RowVersion))
            return new Outcome<ExceptionDecisionResult>.Invalid("rowVersion", "Row version is required for concurrency control.");

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<ExceptionDecisionResult>.Forbidden("Only Lab Coordinators can decide exceptions.");

        var analysis = await _repository.GetByIdAsync(analysisId, ct);
        if (analysis == null)
            return new Outcome<ExceptionDecisionResult>.NotFound($"Analysis {analysisId} not found.");

        var exception = await _repository.GetExceptionByIdAsync(exceptionId, ct);
        if (exception == null)
            return new Outcome<ExceptionDecisionResult>.NotFound($"Exception {exceptionId} not found.");

        if (exception.AnalysisId != analysisId)
            return new Outcome<ExceptionDecisionResult>.Invalid("exceptionId", "Exception does not belong to this analysis.");

        var beforeValues = $"Decision: {exception.Decision}, Comment: {exception.DecisionComment}";

        exception.Decision = request.Decision;
        exception.DecisionComment = request.Comment;
        exception.DecidedByUserId = _currentUser.UserId;
        exception.DecidedAtUtc = _timeProvider.GetUtcNow();

        var expectedRowVersion = Convert.FromBase64String(request.RowVersion);
        var concurrencySuccess = await _repository.TryUpdateExceptionWithConcurrencyCheckAsync(exception, expectedRowVersion, ct);

        if (!concurrencySuccess)
        {
            var refreshedException = await _repository.GetExceptionByIdAsync(exceptionId, ct);
            return new Outcome<ExceptionDecisionResult>.Conflict($"Concurrent modification detected. Current row version is {Convert.ToBase64String(refreshedException!.RowVersion)}");
        }

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = _timeProvider.GetUtcNow(),
            Action = "ExceptionDecided",
            EntityType = "ExceptionRecord",
            EntityId = exception.Id,
            BeforeValues = beforeValues,
            AfterValues = $"Decision: {exception.Decision}, Comment: {exception.DecisionComment}",
        }, ct);

        var refreshed = await _repository.GetExceptionByIdAsync(exceptionId, ct);
        var result = new ExceptionDecisionResult
        {
            Id = refreshed!.Id,
            ReadingId = refreshed.ReadingId,
            Reason = refreshed.Reason,
            Decision = refreshed.Decision,
            DecisionComment = refreshed.DecisionComment,
            RowVersion = Convert.ToBase64String(refreshed.RowVersion),
        };

        return new Outcome<ExceptionDecisionResult>.Ok(result);
    }

    public async Task<Outcome<AnalysisDetailResult>> GetAnalysisDetailAsync(int analysisId, CancellationToken ct)
    {
        var analysis = await _repository.GetByIdAsync(analysisId, ct);
        if (analysis == null)
            return new Outcome<AnalysisDetailResult>.NotFound($"Analysis {analysisId} not found.");

        var usernamesById = new Dictionary<int, string>();
        foreach (var capturerId in analysis.Readings.Select(r => r.CapturedByUserId).Distinct())
        {
            var user = await _userRepository.GetByIdAsync(capturerId, ct);
            if (user != null)
                usernamesById[capturerId] = user.Username;
        }

        var readings = analysis.Readings.Select(r => new ReadingInfo
        {
            Id = r.Id,
            TestId = r.TestId,
            Value = r.Value,
            Unit = r.Unit,
            CapturedAtUtc = r.CapturedAtUtc,
            CapturedByUserId = r.CapturedByUserId,
            CapturedByUsername = usernamesById.TryGetValue(r.CapturedByUserId, out var username)
                ? username
                : r.CapturedByUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ValidationResult = RecomputeValidationDetail(r.Value, r.ValidationResult, analysis.TemplateVersion),
            CalibratedValue = r.CalibratedValue,
        }).ToList();

        var result = new AnalysisDetailResult
        {
            Id = analysis.Id,
            SampleId = analysis.SampleId,
            TemplateId = analysis.TemplateId,
            Status = analysis.Status.ToString(),
            IsLocked = analysis.IsLocked,
            AvailableTests = ParseTests(analysis.TemplateVersion?.TestConfiguration),
            Readings = readings,
            Exceptions = analysis.Exceptions.Select(e => new ExceptionInfo
            {
                Id = e.Id,
                ReadingId = e.ReadingId,
                Reason = e.Reason,
                Decision = e.Decision,
                DecisionComment = e.DecisionComment,
                RowVersion = Convert.ToBase64String(e.RowVersion),
            }).ToList(),
            RowVersion = Convert.ToBase64String(analysis.RowVersion),
        };

        return new Outcome<AnalysisDetailResult>.Ok(result);
    }

    /// <summary>
    /// Creates an ad-hoc (non-scheduled) sample + analysis for the current user's site (R7).
    /// The sample identifier is generated when not supplied, and the analysis is bound to the
    /// template's current version and started immediately so the analyst can capture readings.
    /// </summary>
    public async Task<Outcome<AdHocAnalysisResult>> CreateAdHocAnalysisAsync(int analysisTemplateId, string? sampleIdentifier, CancellationToken ct)
    {
        var template = await _repository.GetTemplateByIdAsync(analysisTemplateId, ct);
        if (template == null)
            return new Outcome<AdHocAnalysisResult>.Invalid("analysisTemplateId", $"Template {analysisTemplateId} not found.");

        if (template.Site != _currentUser.Site)
            return new Outcome<AdHocAnalysisResult>.Invalid("analysisTemplateId", "The template belongs to another site.");

        if (template.IsRetired)
            return new Outcome<AdHocAnalysisResult>.Invalid("analysisTemplateId", "The template is retired and cannot start new analyses.");

        if (template.CurrentVersionId == null)
            return new Outcome<AdHocAnalysisResult>.Invalid("analysisTemplateId", "The template has no active version.");

        string identifier;
        var provided = sampleIdentifier?.Trim();
        if (!string.IsNullOrEmpty(provided))
        {
            if (await _repository.SampleIdentifierExistsAsync(provided, ct))
                return new Outcome<AdHocAnalysisResult>.Invalid("sampleIdentifier", $"Sample identifier '{provided}' is already in use.");
            identifier = provided;
        }
        else
        {
            identifier = await GenerateSampleIdentifierAsync(_currentUser.Site, ct);
        }

        var now = _timeProvider.GetUtcNow();

        var sample = new Sample
        {
            Identifier = identifier,
            AnalysisTemplateId = template.Id,
            Status = LifecycleStatus.InProgress,
            Site = _currentUser.Site,
            CurrentSite = _currentUser.Site,
        };
        await _repository.AddSampleAsync(sample, ct);

        var analysis = new Analysis
        {
            SampleId = sample.Id,
            TemplateId = template.Id,
            TemplateVersionId = template.CurrentVersionId.Value,
            Status = LifecycleStatus.InProgress,
            StartedAtUtc = now,
            StartedByUserId = _currentUser.UserId,
            IsLocked = false,
        };
        await _repository.AddAnalysisAsync(analysis, ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = now,
            Action = "AnalysisCreated",
            EntityType = "Analysis",
            EntityId = analysis.Id,
            AfterValues = $"Sample: {identifier}, Template: {template.Name}",
        }, ct);

        return new Outcome<AdHocAnalysisResult>.Ok(new AdHocAnalysisResult
        {
            AnalysisId = analysis.Id,
            SampleId = sample.Id,
            SampleIdentifier = identifier,
        });
    }

    private async Task<string> GenerateSampleIdentifierAsync(Site site, CancellationToken ct)
    {
        var code = site.ToString().Length >= 3
            ? site.ToString()[..3].ToUpperInvariant()
            : site.ToString().ToUpperInvariant();
        var year = _timeProvider.GetUtcNow().Year;
        var next = await _repository.CountSamplesBySiteAsync(site, ct) + 1;

        string identifier;
        do
        {
            identifier = $"{code}-{year}-{next:D4}";
            next++;
        }
        while (await _repository.SampleIdentifierExistsAsync(identifier, ct));

        return identifier;
    }

    /// <summary>
    /// Parses the tests defined by a template version's TestConfiguration JSON
    /// (shape: {"tests":[{"id":1,"name":"Pol","unit":"°Z","method":"BSES"}]}).
    /// Returns an empty list when the configuration is absent, empty, or malformed -
    /// callers treat "no defined tests" as "accept any test id / unit" for backwards compatibility.
    /// </summary>
    private static List<TestDefinition> ParseTests(string? testConfiguration)
    {
        var tests = new List<TestDefinition>();
        if (string.IsNullOrWhiteSpace(testConfiguration))
            return tests;

        try
        {
            using var doc = JsonDocument.Parse(testConfiguration);
            if (!doc.RootElement.TryGetProperty("tests", out var testsElement)
                || testsElement.ValueKind != JsonValueKind.Array)
                return tests;

            foreach (var element in testsElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                    continue;
                if (!element.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                    continue;

                var name = element.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                    ? nameEl.GetString()!
                    : $"Test {idEl.GetInt32()}";
                var unit = element.TryGetProperty("unit", out var unitEl) && unitEl.ValueKind == JsonValueKind.String
                    ? unitEl.GetString()!
                    : string.Empty;
                var method = element.TryGetProperty("method", out var methodEl) && methodEl.ValueKind == JsonValueKind.String
                    ? methodEl.GetString()
                    : null;

                tests.Add(new TestDefinition
                {
                    Id = idEl.GetInt32(),
                    Name = name,
                    Unit = unit,
                    Method = method,
                });
            }
        }
        catch (JsonException)
        {
            // Malformed configuration is treated as "no defined tests".
            return [];
        }

        return tests;
    }

    private static ReadingValidationDetail RecomputeValidationDetail(decimal value, string persistedStatus, AnalysisTemplateVersion? templateVersion)
    {
        var isValid = persistedStatus == "Valid";
        var actualValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (templateVersion == null)
            return new ReadingValidationDetail
            {
                IsValid = isValid,
                ExpectedRange = "unknown",
                ActualValue = actualValue,
                Reason = null,
            };

        var expectedRange = FormatExpectedRange(templateVersion.MinTolerance, templateVersion.MaxTolerance);
        var reason = isValid ? null : ComputeReason(value, templateVersion.MinTolerance, templateVersion.MaxTolerance);

        return new ReadingValidationDetail
        {
            IsValid = isValid,
            ExpectedRange = expectedRange,
            ActualValue = actualValue,
            Reason = reason,
        };
    }

    private static string? ComputeReason(decimal value, decimal? minTolerance, decimal? maxTolerance)
    {
        if (minTolerance != null && value < minTolerance)
            return $"Reading {value} is below minimum tolerance of {minTolerance}.";

        if (maxTolerance != null && value > maxTolerance)
            return $"Reading {value} is above maximum tolerance of {maxTolerance}.";

        return null;
    }

    public async Task<Outcome<AnalysisStatusChangeResult>> ChangeStatusAsync(int analysisId, StatusChangeRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.Action))
            return new Outcome<AnalysisStatusChangeResult>.Invalid("action", "Action is required.");

        if (string.IsNullOrEmpty(request.RowVersion))
            return new Outcome<AnalysisStatusChangeResult>.Invalid("rowVersion", "Row version is required for concurrency control.");

        var analysis = await _repository.GetByIdAsync(analysisId, ct);
        if (analysis == null)
            return new Outcome<AnalysisStatusChangeResult>.NotFound($"Analysis {analysisId} not found.");

        var beforeStatus = analysis.Status;

        analysis.Status = request.Action switch
        {
            "Start" => LifecycleStatus.InProgress,
            "Pause" => LifecycleStatus.OnHold,
            "Resume" => LifecycleStatus.InProgress,
            "Complete" => LifecycleStatus.Completed,
            "Cancel" => LifecycleStatus.Cancelled,
            _ => analysis.Status,
        };

        if (analysis.Status == LifecycleStatus.Completed)
        {
            analysis.CompletedAtUtc = _timeProvider.GetUtcNow();
            analysis.IsLocked = true;
            analysis.LockedAtUtc = _timeProvider.GetUtcNow();
            analysis.LockedByUserId = _currentUser.UserId;
        }

        var expectedRowVersion = Convert.FromBase64String(request.RowVersion);
        var concurrencySuccess = await _repository.TryUpdateAnalysisWithConcurrencyCheckAsync(analysis, expectedRowVersion, ct);

        if (!concurrencySuccess)
        {
            var refreshedAnalysis = await _repository.GetByIdAsync(analysisId, ct);
            return new Outcome<AnalysisStatusChangeResult>.Conflict($"Concurrent modification detected. Current row version is {Convert.ToBase64String(refreshedAnalysis!.RowVersion)}");
        }

        var afterValues = analysis.Status == LifecycleStatus.Completed
            ? $"Status: {analysis.Status}, IsLocked: {analysis.IsLocked}"
            : $"Status: {analysis.Status}";

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = _timeProvider.GetUtcNow(),
            Action = "StatusChanged",
            EntityType = "Analysis",
            EntityId = analysis.Id,
            BeforeValues = $"Status: {beforeStatus}",
            AfterValues = afterValues,
        }, ct);

        var refreshed = await _repository.GetByIdAsync(analysisId, ct);
        var result = new AnalysisStatusChangeResult
        {
            Id = refreshed!.Id,
            Status = refreshed.Status.ToString(),
            IsLocked = refreshed.IsLocked,
            RowVersion = Convert.ToBase64String(refreshed.RowVersion),
        };

        return new Outcome<AnalysisStatusChangeResult>.Ok(result);
    }

    private static ValidationResult ValidateReading(decimal value, AnalysisTemplateVersion templateVersion)
    {
        var expectedRange = FormatExpectedRange(templateVersion.MinTolerance, templateVersion.MaxTolerance);
        var actualValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (templateVersion.MinTolerance != null && value < templateVersion.MinTolerance)
            return new ValidationResult
            {
                Status = "OutOfTolerance",
                IsOutOfTolerance = true,
                Reason = $"Reading {value} is below minimum tolerance of {templateVersion.MinTolerance}.",
                ExpectedRange = expectedRange,
                ActualValue = actualValue,
            };

        if (templateVersion.MaxTolerance != null && value > templateVersion.MaxTolerance)
            return new ValidationResult
            {
                Status = "OutOfTolerance",
                IsOutOfTolerance = true,
                Reason = $"Reading {value} is above maximum tolerance of {templateVersion.MaxTolerance}.",
                ExpectedRange = expectedRange,
                ActualValue = actualValue,
            };

        return new ValidationResult
        {
            Status = "Valid",
            IsOutOfTolerance = false,
            ExpectedRange = expectedRange,
            ActualValue = actualValue,
        };
    }

    private static string FormatExpectedRange(decimal? minTolerance, decimal? maxTolerance)
    {
        if (minTolerance != null && maxTolerance != null)
            return $"{minTolerance}-{maxTolerance}";
        else if (minTolerance != null)
            return $"above {minTolerance}";
        else if (maxTolerance != null)
            return $"below {maxTolerance}";
        else
            return "no limits";
    }
}

public sealed record CaptureReadingRequest
{
    public required int TestId { get; init; }
    public required decimal Value { get; init; }
    public required string Unit { get; init; }
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public int? InstrumentId { get; init; }
}

public sealed record ExceptionDecisionRequest
{
    public required string Decision { get; init; }
    public required string Comment { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record StatusChangeRequest
{
    public required string Action { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record ReadingValidationDetail
{
    public required bool IsValid { get; init; }
    public string? ExpectedRange { get; init; }
    public required string ActualValue { get; init; }
    public string? Reason { get; init; }
}

public sealed record ReadingCaptureResult
{
    public required int Id { get; init; }
    public required int TestId { get; init; }
    public required decimal Value { get; init; }
    public required string Unit { get; init; }
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public required int CapturedByUserId { get; init; }
    public required string CapturedByUsername { get; init; }
    public required ReadingValidationDetail ValidationResult { get; init; }
    public decimal? CalibratedValue { get; init; }
}

public sealed record ExceptionDecisionResult
{
    public required int Id { get; init; }
    public required int ReadingId { get; init; }
    public required string Reason { get; init; }
    public string? Decision { get; init; }
    public string? DecisionComment { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record AnalysisStatusChangeResult
{
    public required int Id { get; init; }
    public required string Status { get; init; }
    public required bool IsLocked { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record AnalysisDetailResult
{
    public required int Id { get; init; }
    public required int SampleId { get; init; }
    public required int TemplateId { get; init; }
    public required string Status { get; init; }
    public required bool IsLocked { get; init; }
    public required List<TestDefinition> AvailableTests { get; init; }
    public required List<ReadingInfo> Readings { get; init; }
    public required List<ExceptionInfo> Exceptions { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record TestDefinition
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Unit { get; init; }
    public string? Method { get; init; }
}

public sealed record AdHocAnalysisResult
{
    public required int AnalysisId { get; init; }
    public required int SampleId { get; init; }
    public required string SampleIdentifier { get; init; }
}

public sealed record ReadingInfo
{
    public required int Id { get; init; }
    public required int TestId { get; init; }
    public required decimal Value { get; init; }
    public required string Unit { get; init; }
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public required int CapturedByUserId { get; init; }
    public required string CapturedByUsername { get; init; }
    public required ReadingValidationDetail ValidationResult { get; init; }
    public decimal? CalibratedValue { get; init; }
}

public sealed record ExceptionInfo
{
    public required int Id { get; init; }
    public required int ReadingId { get; init; }
    public required string Reason { get; init; }
    public string? Decision { get; init; }
    public string? DecisionComment { get; init; }
    public required string RowVersion { get; init; }
}

internal sealed record ValidationResult
{
    public required string Status { get; init; }
    public required bool IsOutOfTolerance { get; init; }
    public string? Reason { get; init; }
    public string? ExpectedRange { get; init; }
    public string? ActualValue { get; init; }
}
