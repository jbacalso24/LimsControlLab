using LimsControlLab.Domain.Entities;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure;

/// <summary>
/// Illustrative development seed data (FidelityLabel: Illustrative). Runs only in Development,
/// only when the database is empty. Values are drawn from the organisation's real reference
/// workbook and BRD domain vocabulary (sites, products, tests, methods, instruments) so the
/// screens demonstrate real behaviour against backend-served data — no mock data lives in the
/// frontend. Ids are always read back after SaveChanges before being used as FKs (never hardcoded).
/// </summary>
public static class SeedData
{
    public static async Task SeedIfEmptyAsync(
        LimsDbContext db,
        Func<string, string>? passwordHasher = null,
        CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
            return;

        var hasher = passwordHasher ?? (pwd => pwd);
        var baseDate = new DateTimeOffset(new DateTime(2026, 8, 24), TimeSpan.Zero);

        // ---- Users: a coordinator + analyst for four representative mill sites ----
        var users = new[]
        {
            new User { Username = "inkerman_coord", PasswordHash = hasher("inkerman_coord_password"), Role = Role.LabCoordinator, Site = Site.Inkerman },
            new User { Username = "inkerman_analyst", PasswordHash = hasher("inkerman_analyst_password"), Role = Role.ControlLabAnalyst, Site = Site.Inkerman },
            new User { Username = "invicta_coord", PasswordHash = hasher("invicta_coord_password"), Role = Role.LabCoordinator, Site = Site.Invicta },
            new User { Username = "invicta_analyst", PasswordHash = hasher("invicta_analyst_password"), Role = Role.ControlLabAnalyst, Site = Site.Invicta },
            new User { Username = "kalamia_coord", PasswordHash = hasher("kalamia_coord_password"), Role = Role.LabCoordinator, Site = Site.Kalamia },
            new User { Username = "kalamia_analyst", PasswordHash = hasher("kalamia_analyst_password"), Role = Role.ControlLabAnalyst, Site = Site.Kalamia },
            new User { Username = "pioneer_coord", PasswordHash = hasher("pioneer_coord_password"), Role = Role.LabCoordinator, Site = Site.Pioneer },
            new User { Username = "pioneer_analyst", PasswordHash = hasher("pioneer_analyst_password"), Role = Role.ControlLabAnalyst, Site = Site.Pioneer },
        };
        await db.Users.AddRangeAsync(users, ct);
        await db.SaveChangesAsync(ct);

        var userId = await db.Users.ToDictionaryAsync(u => u.Username, u => u.Id, ct);
        int InkAnalyst = userId["inkerman_analyst"], InkCoord = userId["inkerman_coord"];
        int InvAnalyst = userId["invicta_analyst"], InvCoord = userId["invicta_coord"];
        int KalAnalyst = userId["kalamia_analyst"];

        // ---- Instruments: real Control-Lab instrument classes per site (BRD R26-R28) ----
        Instrument Inst(string name, string model, string serial, Site site) =>
            new() { Name = name, Model = model, SerialNumber = serial, Site = site, IsActive = true };
        var instruments = new[]
        {
            Inst("Polarimeter", "Schmidt+Haensch Polartronic NIR-2W", "POL-INK-01", Site.Inkerman),
            Inst("Refractometer", "Atago RX-5000", "REF-INK-01", Site.Inkerman),
            Inst("Analytical Balance", "Mettler Toledo AE260", "BAL-INK-01", Site.Inkerman),
            Inst("HPLC", "Agilent 1260 Infinity", "HPLC-INK-01", Site.Inkerman),
            Inst("Polarimeter", "Schmidt+Haensch Polartronic Universal", "POL-INV-01", Site.Invicta),
            Inst("Refractometer", "Atago PR-32", "REF-INV-01", Site.Invicta),
            Inst("pH Meter", "Eutech Cyberscan pH500", "PH-INV-01", Site.Invicta),
            Inst("Analytical Balance", "Mettler Toledo PM4800", "BAL-INV-01", Site.Invicta),
            Inst("Polarimeter", "Schmidt+Haensch Polartronic", "POL-KAL-01", Site.Kalamia),
            Inst("Refractometer", "Atago RX-5000", "REF-KAL-01", Site.Kalamia),
            Inst("Polarimeter", "Schmidt+Haensch Polartronic", "POL-PIO-01", Site.Pioneer),
            Inst("Refractometer", "Atago PR-32", "REF-PIO-01", Site.Pioneer),
        };
        await db.Instruments.AddRangeAsync(instruments, ct);

        // ---- Analysis templates with real test/validation/calculation configuration ----
        var templates = new[]
        {
            new AnalysisTemplate { Name = "Sugar Pol (BSES)", Site = Site.Inkerman, IsRetired = false, MinTolerance = 98.0m, MaxTolerance = 99.8m },
            new AnalysisTemplate { Name = "Sugar Brix (Refractometer)", Site = Site.Inkerman, IsRetired = false, MinTolerance = 99.0m, MaxTolerance = 99.9m },
            new AnalysisTemplate { Name = "Sugar Water (BSES)", Site = Site.Inkerman, IsRetired = false, MinTolerance = 0.0m, MaxTolerance = 0.15m },
            new AnalysisTemplate { Name = "Final Molasses Purity", Site = Site.Invicta, IsRetired = false, MinTolerance = 28.0m, MaxTolerance = 40.0m },
            new AnalysisTemplate { Name = "A Massecuite Brix", Site = Site.Invicta, IsRetired = false, MinTolerance = 91.0m, MaxTolerance = 94.0m },
            new AnalysisTemplate { Name = "Mud Pol", Site = Site.Kalamia, IsRetired = false, MinTolerance = 0.5m, MaxTolerance = 3.0m },
            new AnalysisTemplate { Name = "Sugar Pol (BSES) - Legacy", Site = Site.Inkerman, IsRetired = true, MinTolerance = 98.0m, MaxTolerance = 99.5m },
        };
        await db.AnalysisTemplates.AddRangeAsync(templates, ct);
        await db.SaveChangesAsync(ct);

        var tpl = await db.AnalysisTemplates.OrderBy(t => t.Id).ToListAsync(ct);

        // Illustrative JSON config strings (BRD R1: tests, readings, calculations, validation rules).
        string PolConfig = "{\"tests\":[{\"id\":1,\"name\":\"Pol\",\"unit\":\"°Z\",\"method\":\"BSES\"},{\"id\":2,\"name\":\"Temperature\",\"unit\":\"°C\"}],\"sampleMethod\":\"Single (snap)\"}";
        string PolCalc = "{\"formula\":\"pol_corrected = pol * tempFactor(temperature)\",\"type\":\"calibration\"}";
        string PolValidation = "{\"rules\":[{\"field\":\"Pol\",\"min\":98.0,\"max\":99.8,\"type\":\"tolerance\"},{\"sequence\":[\"Brix\",\"Temperature\"]}]}";
        string MolValidation = "{\"rules\":[{\"crossField\":\"AMol > BMol > CMol\",\"type\":\"relationship\"},{\"field\":\"Purity\",\"min\":28,\"max\":40}]}";

        var versions = new[]
        {
            new AnalysisTemplateVersion { TemplateId = tpl[0].Id, Version = 1, MinTolerance = 98.0m, MaxTolerance = 99.8m, TestConfiguration = PolConfig, CalculationDefinitions = PolCalc, ValidationRules = PolValidation, CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[1].Id, Version = 1, MinTolerance = 99.0m, MaxTolerance = 99.9m, TestConfiguration = "{\"tests\":[{\"id\":3,\"name\":\"Brix\",\"unit\":\"°Bx\",\"method\":\"Refractometer\"}],\"sampleMethod\":\"Single (snap)\"}", CalculationDefinitions = "{\"formula\":\"brix = refractometer_reading\"}", ValidationRules = "{\"rules\":[{\"field\":\"Brix\",\"min\":99.0,\"max\":99.9}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[2].Id, Version = 1, MinTolerance = 0.0m, MaxTolerance = 0.15m, TestConfiguration = "{\"tests\":[{\"id\":4,\"name\":\"Water\",\"unit\":\"%\",\"method\":\"BSES\"}],\"sampleMethod\":\"Single (snap)\"}", CalculationDefinitions = "{\"formula\":\"moisture = (wet - dry) / wet * 100\",\"type\":\"weighted_average\"}", ValidationRules = "{\"rules\":[{\"field\":\"Water\",\"min\":0.0,\"max\":0.15}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[3].Id, Version = 1, MinTolerance = 28.0m, MaxTolerance = 40.0m, TestConfiguration = "{\"tests\":[{\"id\":5,\"name\":\"Purity\",\"unit\":\"%\"},{\"id\":6,\"name\":\"Pol\",\"unit\":\"°Z\"},{\"id\":7,\"name\":\"Brix\",\"unit\":\"°Bx\"}],\"sampleMethod\":\"Composite\"}", CalculationDefinitions = "{\"formula\":\"purity = pol / brix * 100\"}", ValidationRules = MolValidation, CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[4].Id, Version = 1, MinTolerance = 91.0m, MaxTolerance = 94.0m, TestConfiguration = "{\"tests\":[{\"id\":3,\"name\":\"Brix\",\"unit\":\"°Bx\"}],\"sampleMethod\":\"Single (snap)\"}", CalculationDefinitions = "{}", ValidationRules = "{\"rules\":[{\"field\":\"Brix\",\"min\":91,\"max\":94}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[5].Id, Version = 1, MinTolerance = 0.5m, MaxTolerance = 3.0m, TestConfiguration = "{\"tests\":[{\"id\":1,\"name\":\"Pol\",\"unit\":\"°Z\"}],\"sampleMethod\":\"Single (snap)\"}", CalculationDefinitions = "{}", ValidationRules = "{\"rules\":[{\"field\":\"Pol\",\"min\":0.5,\"max\":3.0}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[6].Id, Version = 1, MinTolerance = 98.0m, MaxTolerance = 99.5m, TestConfiguration = PolConfig, CalculationDefinitions = PolCalc, ValidationRules = PolValidation, CreatedAtUtc = baseDate.AddYears(-1) },
        };
        await db.AnalysisTemplateVersions.AddRangeAsync(versions, ct);
        await db.SaveChangesAsync(ct);

        for (int i = 0; i < tpl.Count; i++) tpl[i].CurrentVersionId = versions[i].Id;
        db.AnalysisTemplates.UpdateRange(tpl);
        await db.SaveChangesAsync(ct);

        // ---- Schedules with real recurrence + exclusion patterns (BRD R8-R11) ----
        var schedules = new[]
        {
            new Schedule { Name = "Sugar Pol - Every 2h (Day)", Site = Site.Inkerman, AnalysisType = "Sugar Pol (BSES)", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Every 2 hours during shift", ExclusionRules = "Not during scheduled maintenance", AssignedToUserId = InkAnalyst, IsActive = true },
            new Schedule { Name = "Sugar Brix - Hourly", Site = Site.Inkerman, AnalysisType = "Sugar Brix (Refractometer)", ShiftPattern = ShiftPattern.Shift, RecurrencePattern = "Hourly", ExclusionRules = null, AssignedToUserId = InkAnalyst, IsActive = true },
            new Schedule { Name = "Final Molasses - Per shift", Site = Site.Invicta, AnalysisType = "Final Molasses Purity", ShiftPattern = ShiftPattern.Shift, RecurrencePattern = "Once per shift", ExclusionRules = "Excludes Sundays", AssignedToUserId = InvAnalyst, IsActive = true },
            new Schedule { Name = "A Massecuite - Weekly QA", Site = Site.Invicta, AnalysisType = "A Massecuite Brix", ShiftPattern = ShiftPattern.Weekly, RecurrencePattern = "Weekly on Monday", ExclusionRules = null, AssignedToUserId = null, IsActive = true },
            new Schedule { Name = "Mud Pol - Day (suspended)", Site = Site.Kalamia, AnalysisType = "Mud Pol", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Every 4 hours", ExclusionRules = "Suspended during factory stoppage", AssignedToUserId = KalAnalyst, IsActive = false },
        };
        await db.Schedules.AddRangeAsync(schedules, ct);

        // ---- Samples across sites/statuses ----
        Sample Smp(string ident, int templateId, LifecycleStatus status, Site site) =>
            new() { Identifier = ident, AnalysisTemplateId = templateId, Status = status, Site = site, CurrentSite = site };
        var samples = new[]
        {
            Smp("INK-2026-0001", tpl[0].Id, LifecycleStatus.InProgress, Site.Inkerman),
            Smp("INK-2026-0002", tpl[0].Id, LifecycleStatus.Completed, Site.Inkerman),
            Smp("INK-2026-0003", tpl[1].Id, LifecycleStatus.NotStarted, Site.Inkerman),
            Smp("INK-2026-0004", tpl[2].Id, LifecycleStatus.OnHold, Site.Inkerman),
            Smp("INV-2026-0001", tpl[3].Id, LifecycleStatus.InProgress, Site.Invicta),
            Smp("INV-2026-0002", tpl[4].Id, LifecycleStatus.Completed, Site.Invicta),
            Smp("INV-2026-0003", tpl[3].Id, LifecycleStatus.Cancelled, Site.Invicta),
            Smp("KAL-2026-0001", tpl[5].Id, LifecycleStatus.NotStarted, Site.Kalamia),
        };
        await db.Samples.AddRangeAsync(samples, ct);
        await db.SaveChangesAsync(ct);

        var instId = await db.Instruments.OrderBy(i => i.Id).ToListAsync(ct);
        int PolInk = instId[0].Id, RefInk = instId[1].Id, PolInv = instId[4].Id;

        // ---- Analyses across all five lifecycle statuses ----
        Analysis An(int sampleId, int tplIdx, LifecycleStatus status, int startedBy, DateTimeOffset started,
            bool locked = false, DateTimeOffset? completed = null, int? lockedBy = null) =>
            new()
            {
                SampleId = sampleId, TemplateId = tpl[tplIdx].Id, TemplateVersionId = versions[tplIdx].Id,
                Status = status, StartedAtUtc = started, StartedByUserId = startedBy,
                IsLocked = locked, CompletedAtUtc = completed,
                LockedAtUtc = locked ? completed : null, LockedByUserId = lockedBy,
            };
        var analyses = new[]
        {
            An(samples[0].Id, 0, LifecycleStatus.InProgress, InkAnalyst, baseDate.AddHours(2)),
            An(samples[1].Id, 0, LifecycleStatus.Completed, InkAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(3), lockedBy: InkCoord),
            An(samples[3].Id, 2, LifecycleStatus.OnHold, InkAnalyst, baseDate.AddHours(1)),
            An(samples[4].Id, 3, LifecycleStatus.InProgress, InvAnalyst, baseDate.AddHours(4)),
            An(samples[5].Id, 4, LifecycleStatus.Completed, InvAnalyst, baseDate.AddDays(-2), locked: true, completed: baseDate.AddDays(-2).AddHours(2), lockedBy: InvCoord),
            An(samples[6].Id, 3, LifecycleStatus.Cancelled, InvAnalyst, baseDate.AddDays(-1)),
        };
        await db.Analyses.AddRangeAsync(analyses, ct);
        await db.SaveChangesAsync(ct);

        // ---- Readings (in- and out-of-tolerance) ----
        Reading Rdg(int analysisId, int testId, decimal value, string unit, int by, int? instrument, string validation, decimal? calibrated, DateTimeOffset at) =>
            new() { AnalysisId = analysisId, TestId = testId, Value = value, Unit = unit, CapturedByUserId = by, InstrumentId = instrument, ValidationResult = validation, CalibratedValue = calibrated, CapturedAtUtc = at };
        var readings = new[]
        {
            // In-progress Pol analysis: one valid Pol reading, one out-of-tolerance
            Rdg(analyses[0].Id, 1, 99.1m, "°Z", InkAnalyst, PolInk, "Valid", 99.0m, baseDate.AddHours(2).AddMinutes(10)),
            Rdg(analyses[0].Id, 2, 27.5m, "°C", InkAnalyst, null, "Valid", null, baseDate.AddHours(2).AddMinutes(12)),
            Rdg(analyses[0].Id, 1, 97.2m, "°Z", InkAnalyst, PolInk, "OutOfTolerance", 97.1m, baseDate.AddHours(2).AddMinutes(40)),
            // Completed+locked analysis: valid readings
            Rdg(analyses[1].Id, 1, 99.4m, "°Z", InkAnalyst, PolInk, "Valid", 99.3m, baseDate.AddDays(-1).AddHours(1)),
            Rdg(analyses[1].Id, 2, 26.0m, "°C", InkAnalyst, null, "Valid", null, baseDate.AddDays(-1).AddHours(1)),
            // Invicta molasses in-progress: out-of-tolerance purity
            Rdg(analyses[3].Id, 5, 25.0m, "%", InvAnalyst, PolInv, "OutOfTolerance", null, baseDate.AddHours(4).AddMinutes(20)),
            // Invicta completed massecuite
            Rdg(analyses[4].Id, 3, 92.5m, "°Bx", InvAnalyst, PolInv, "Valid", null, baseDate.AddDays(-2).AddHours(1)),
        };
        await db.Readings.AddRangeAsync(readings, ct);
        await db.SaveChangesAsync(ct);

        var rdgList = await db.Readings.OrderBy(r => r.Id).ToListAsync(ct);
        int outOfTolInk = rdgList[2].Id;     // the 97.2 Pol reading
        int outOfTolInv = rdgList[5].Id;     // the 25.0 purity reading

        // ---- Exceptions: one open (awaiting decision), one resolved ----
        var exceptions = new[]
        {
            new ExceptionRecord { AnalysisId = analyses[0].Id, ReadingId = outOfTolInk, Reason = "Pol 97.2 °Z below expected range 98.0-99.8", Decision = null },
            new ExceptionRecord { AnalysisId = analyses[3].Id, ReadingId = outOfTolInv, Reason = "Final molasses purity 25.0% below minimum 28.0%", Decision = "AcceptWithComment", DecisionComment = "Confirmed low-purity C molasses batch; accepted per shift coordinator.", DecidedByUserId = InvCoord, DecidedAtUtc = baseDate.AddHours(5) },
        };
        await db.ExceptionRecords.AddRangeAsync(exceptions, ct);
        await db.SaveChangesAsync(ct);

        // ---- Calibration curves (linear standards) ----
        var curves = new[]
        {
            new CalibrationCurve { Name = "Polarimeter Standard - Inkerman", AnalysisTemplateId = tpl[0].Id, IsActive = true },
            new CalibrationCurve { Name = "Refractometer Standard - Inkerman", AnalysisTemplateId = tpl[1].Id, IsActive = true },
            new CalibrationCurve { Name = "Polarimeter Standard - Invicta", AnalysisTemplateId = tpl[3].Id, IsActive = true },
        };
        await db.CalibrationCurves.AddRangeAsync(curves, ct);
        await db.SaveChangesAsync(ct);

        var curveList = await db.CalibrationCurves.OrderBy(c => c.Id).ToListAsync(ct);
        var points = new[]
        {
            new CalibrationPoint { CalibrationCurveId = curveList[0].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[0].Id, XValue = 50m, YValue = 49.9m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[0].Id, XValue = 100m, YValue = 99.8m, Order = 2 },
            new CalibrationPoint { CalibrationCurveId = curveList[1].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[1].Id, XValue = 50m, YValue = 50.5m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[1].Id, XValue = 100m, YValue = 100m, Order = 2 },
            new CalibrationPoint { CalibrationCurveId = curveList[2].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[2].Id, XValue = 50m, YValue = 51m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[2].Id, XValue = 100m, YValue = 100m, Order = 2 },
        };
        await db.CalibrationPoints.AddRangeAsync(points, ct);
        await db.SaveChangesAsync(ct);
    }
}
