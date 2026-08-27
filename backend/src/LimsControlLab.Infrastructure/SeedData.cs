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

        await SeedAsync(db, hasher, ct);
    }

    public static async Task ResetAndReseedAsync(
        LimsDbContext db,
        Func<string, string> hasher,
        CancellationToken ct = default)
    {
        // Break the AnalysisTemplate <-> AnalysisTemplateVersion cycle first: a template's
        // CurrentVersionId FK must be cleared before its versions can be deleted.
        var templates = await db.AnalysisTemplates.ToListAsync(ct);
        foreach (var t in templates)
            t.CurrentVersionId = null;
        await db.SaveChangesAsync(ct);

        // Delete every table in FK-safe order (children before parents). ExceptionRecords
        // reference Readings AND Analyses, so they must be removed before either.
        // ExceptionRecords, Readings and IntegrationLogs all reference Analyses, so they go first.
        await db.ExceptionRecords.ExecuteDeleteAsync(ct);
        await db.Readings.ExecuteDeleteAsync(ct);
        await db.IntegrationLogs.ExecuteDeleteAsync(ct);
        await db.CalibrationPoints.ExecuteDeleteAsync(ct);
        await db.CalibrationCurves.ExecuteDeleteAsync(ct);
        await db.SampleTransfers.ExecuteDeleteAsync(ct);
        await db.Analyses.ExecuteDeleteAsync(ct);
        await db.Samples.ExecuteDeleteAsync(ct);
        await db.Schedules.ExecuteDeleteAsync(ct);
        await db.AnalysisTemplateVersions.ExecuteDeleteAsync(ct);
        await db.AnalysisTemplates.ExecuteDeleteAsync(ct);
        await db.SamplingMethods.ExecuteDeleteAsync(ct);
        await db.Instruments.ExecuteDeleteAsync(ct);
        await db.AuditLogs.ExecuteDeleteAsync(ct);
        await db.Users.ExecuteDeleteAsync(ct);

        // ExecuteDelete bypasses the change tracker; clear it so stale entities don't
        // interfere with the fresh insert graph.
        db.ChangeTracker.Clear();

        await SeedAsync(db, hasher, ct);
    }

    private static async Task SeedAsync(
        LimsDbContext db,
        Func<string, string> hasher,
        CancellationToken ct)
    {
        var baseDate = new DateTimeOffset(new DateTime(2026, 8, 24), TimeSpan.Zero);

        // ---- Users: coordinators + analysts for 6 sites (Inkerman, Invicta, Kalamia, Pioneer, Victoria, Macknade) ----
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
            new User { Username = "victoria_coord", PasswordHash = hasher("victoria_coord_password"), Role = Role.LabCoordinator, Site = Site.Victoria },
            new User { Username = "victoria_analyst", PasswordHash = hasher("victoria_analyst_password"), Role = Role.ControlLabAnalyst, Site = Site.Victoria },
            new User { Username = "macknade_coord", PasswordHash = hasher("macknade_coord_password"), Role = Role.LabCoordinator, Site = Site.Macknade },
            new User { Username = "macknade_analyst", PasswordHash = hasher("macknade_analyst_password"), Role = Role.ControlLabAnalyst, Site = Site.Macknade },
        };
        await db.Users.AddRangeAsync(users, ct);
        await db.SaveChangesAsync(ct);

        var userId = await db.Users.ToDictionaryAsync(u => u.Username, u => u.Id, ct);
        int InkAnalyst = userId["inkerman_analyst"], InkCoord = userId["inkerman_coord"];
        int InvAnalyst = userId["invicta_analyst"], InvCoord = userId["invicta_coord"];
        int KalAnalyst = userId["kalamia_analyst"], KalCoord = userId["kalamia_coord"];
        int PioAnalyst = userId["pioneer_analyst"], PioCoord = userId["pioneer_coord"];
        int VicAnalyst = userId["victoria_analyst"], VicCoord = userId["victoria_coord"];
        int MacAnalyst = userId["macknade_analyst"], MacCoord = userId["macknade_coord"];

        // ---- Instruments: 4-6 per site across all 6 seeded sites ----
        Instrument Inst(string name, string model, string serial, Site site) =>
            new() { Name = name, Model = model, SerialNumber = serial, Site = site, IsActive = true };
        var instruments = new[]
        {
            // Inkerman (6)
            Inst("Polarimeter", "Schmidt+Haensch Polartronic NIR-2W", "POL-INK-01", Site.Inkerman),
            Inst("Refractometer", "Atago RX-5000", "REF-INK-01", Site.Inkerman),
            Inst("Analytical Balance", "Mettler Toledo AE260", "BAL-INK-01", Site.Inkerman),
            Inst("HPLC", "Agilent 1260 Infinity", "HPLC-INK-01", Site.Inkerman),
            Inst("Spectrophotometer", "Shimadzu UV-1800", "SPEC-INK-01", Site.Inkerman),
            Inst("pH Meter", "Eutech Cyberscan pH500", "PH-INK-01", Site.Inkerman),
            // Invicta (5)
            Inst("Polarimeter", "Schmidt+Haensch Polartronic Universal", "POL-INV-01", Site.Invicta),
            Inst("Refractometer", "Atago PR-32", "REF-INV-01", Site.Invicta),
            Inst("pH Meter", "Eutech Cyberscan pH500", "PH-INV-01", Site.Invicta),
            Inst("Analytical Balance", "Mettler Toledo PM4800", "BAL-INV-01", Site.Invicta),
            Inst("HPLC", "Agilent 1290 Infinity II", "HPLC-INV-01", Site.Invicta),
            // Kalamia (4)
            Inst("Polarimeter", "Schmidt+Haensch Polartronic", "POL-KAL-01", Site.Kalamia),
            Inst("Refractometer", "Atago RX-5000", "REF-KAL-01", Site.Kalamia),
            Inst("Analytical Balance", "Sartorius ML-T", "BAL-KAL-01", Site.Kalamia),
            Inst("HPLC", "Shimadzu Nexera X2", "HPLC-KAL-01", Site.Kalamia),
            // Pioneer (4)
            Inst("Polarimeter", "Schmidt+Haensch Polartronic", "POL-PIO-01", Site.Pioneer),
            Inst("Refractometer", "Atago PR-32", "REF-PIO-01", Site.Pioneer),
            Inst("pH Meter", "Thermo Scientific Orion", "PH-PIO-01", Site.Pioneer),
            Inst("Analytical Balance", "Mettler Toledo AE260", "BAL-PIO-01", Site.Pioneer),
            // Victoria (4)
            Inst("Polarimeter", "Schmidt+Haensch Polartronic NIR-2W", "POL-VIC-01", Site.Victoria),
            Inst("Refractometer", "Atago RX-5000", "REF-VIC-01", Site.Victoria),
            Inst("Spectrophotometer", "Thermo Scientific Genesys", "SPEC-VIC-01", Site.Victoria),
            Inst("HPLC", "Agilent 1260 Infinity", "HPLC-VIC-01", Site.Victoria),
            // Macknade (5)
            Inst("Polarimeter", "Schmidt+Haensch Polartronic Universal", "POL-MAC-01", Site.Macknade),
            Inst("Refractometer", "Atago PR-32", "REF-MAC-01", Site.Macknade),
            Inst("Analytical Balance", "Sartorius MCA", "BAL-MAC-01", Site.Macknade),
            Inst("HPLC", "Shimadzu Nexera", "HPLC-MAC-01", Site.Macknade),
            Inst("pH Meter", "Eutech Cyberscan pH500", "PH-MAC-01", Site.Macknade),
        };
        await db.Instruments.AddRangeAsync(instruments, ct);

        // ---- SamplingMethods: populate per site with realistic methods ----
        var samplingMethods = new[]
        {
            // Inkerman
            new SamplingMethod { Name = "Single (snap)", Site = Site.Inkerman, Description = "Single instantaneous grab sample", IsActive = true },
            new SamplingMethod { Name = "Composite", Site = Site.Inkerman, Description = "Equal volume composite sample", IsActive = true },
            new SamplingMethod { Name = "Combined", Site = Site.Inkerman, Description = "Multiple time-point combined sample", IsActive = true },
            new SamplingMethod { Name = "Split", Site = Site.Inkerman, Description = "Parallel split samples for duplicate analysis", IsActive = false },
            // Invicta
            new SamplingMethod { Name = "Single (snap)", Site = Site.Invicta, Description = "Single instantaneous grab sample", IsActive = true },
            new SamplingMethod { Name = "Composite", Site = Site.Invicta, Description = "Equal volume composite sample", IsActive = true },
            new SamplingMethod { Name = "Exchange", Site = Site.Invicta, Description = "Exchange method for comparison", IsActive = true },
            // Kalamia
            new SamplingMethod { Name = "Single (snap)", Site = Site.Kalamia, Description = "Single instantaneous grab sample", IsActive = true },
            new SamplingMethod { Name = "Composite", Site = Site.Kalamia, Description = "Equal volume composite sample", IsActive = true },
            new SamplingMethod { Name = "Combined", Site = Site.Kalamia, Description = "Multiple time-point combined sample", IsActive = true },
            // Pioneer
            new SamplingMethod { Name = "Single (snap)", Site = Site.Pioneer, Description = "Single instantaneous grab sample", IsActive = true },
            new SamplingMethod { Name = "Composite", Site = Site.Pioneer, Description = "Equal volume composite sample", IsActive = true },
            // Victoria
            new SamplingMethod { Name = "Single (snap)", Site = Site.Victoria, Description = "Single instantaneous grab sample", IsActive = true },
            new SamplingMethod { Name = "Composite", Site = Site.Victoria, Description = "Equal volume composite sample", IsActive = true },
            new SamplingMethod { Name = "Combined", Site = Site.Victoria, Description = "Multiple time-point combined sample", IsActive = true },
            new SamplingMethod { Name = "Exchange", Site = Site.Victoria, Description = "Exchange method for comparison", IsActive = false },
            // Macknade
            new SamplingMethod { Name = "Single (snap)", Site = Site.Macknade, Description = "Single instantaneous grab sample", IsActive = true },
            new SamplingMethod { Name = "Composite", Site = Site.Macknade, Description = "Equal volume composite sample", IsActive = true },
            new SamplingMethod { Name = "Split", Site = Site.Macknade, Description = "Parallel split samples for duplicate analysis", IsActive = true },
        };
        await db.SamplingMethods.AddRangeAsync(samplingMethods, ct);

        // ---- Analysis templates with real test/validation/calculation configuration ----
        var templates = new[]
        {
            // Inkerman templates
            new AnalysisTemplate { Name = "Sugar Pol (BSES)", Site = Site.Inkerman, IsRetired = false, MinTolerance = 98.0m, MaxTolerance = 99.8m },
            new AnalysisTemplate { Name = "Sugar Brix (Refractometer)", Site = Site.Inkerman, IsRetired = false, MinTolerance = 99.0m, MaxTolerance = 99.9m },
            new AnalysisTemplate { Name = "Sugar Water (BSES)", Site = Site.Inkerman, IsRetired = false, MinTolerance = 0.0m, MaxTolerance = 0.15m },
            new AnalysisTemplate { Name = "Raw Sugar Colour", Site = Site.Inkerman, IsRetired = false, MinTolerance = 100m, MaxTolerance = 1000m },
            new AnalysisTemplate { Name = "Sugar Pol (BSES) - Legacy", Site = Site.Inkerman, IsRetired = true, MinTolerance = 98.0m, MaxTolerance = 99.5m },
            // Invicta templates
            new AnalysisTemplate { Name = "Final Molasses Purity", Site = Site.Invicta, IsRetired = false, MinTolerance = 28.0m, MaxTolerance = 40.0m },
            new AnalysisTemplate { Name = "A Massecuite Brix", Site = Site.Invicta, IsRetired = false, MinTolerance = 91.0m, MaxTolerance = 94.0m },
            new AnalysisTemplate { Name = "B Massecuite Brix", Site = Site.Invicta, IsRetired = false, MinTolerance = 85.0m, MaxTolerance = 88.0m },
            new AnalysisTemplate { Name = "C Massecuite Brix", Site = Site.Invicta, IsRetired = false, MinTolerance = 75.0m, MaxTolerance = 78.0m },
            new AnalysisTemplate { Name = "Juice Purity", Site = Site.Invicta, IsRetired = false, MinTolerance = 82.0m, MaxTolerance = 88.0m },
            // Kalamia templates
            new AnalysisTemplate { Name = "Mud Pol", Site = Site.Kalamia, IsRetired = false, MinTolerance = 0.5m, MaxTolerance = 3.0m },
            new AnalysisTemplate { Name = "Ash Content", Site = Site.Kalamia, IsRetired = false, MinTolerance = 1.5m, MaxTolerance = 3.5m },
            new AnalysisTemplate { Name = "Pol Recovery", Site = Site.Kalamia, IsRetired = false, MinTolerance = 85.0m, MaxTolerance = 95.0m },
            // Pioneer templates
            new AnalysisTemplate { Name = "Final Sugar Pol", Site = Site.Pioneer, IsRetired = false, MinTolerance = 99.5m, MaxTolerance = 99.9m },
            new AnalysisTemplate { Name = "Final Sugar Brix", Site = Site.Pioneer, IsRetired = false, MinTolerance = 98.0m, MaxTolerance = 99.0m },
            // Victoria templates
            new AnalysisTemplate { Name = "Cane Quality Pol", Site = Site.Victoria, IsRetired = false, MinTolerance = 12.0m, MaxTolerance = 14.0m },
            new AnalysisTemplate { Name = "Plant Fibre Content", Site = Site.Victoria, IsRetired = false, MinTolerance = 10.0m, MaxTolerance = 14.0m },
            // Macknade templates
            new AnalysisTemplate { Name = "Sugar Pol (BSES)", Site = Site.Macknade, IsRetired = false, MinTolerance = 98.0m, MaxTolerance = 99.8m },
            new AnalysisTemplate { Name = "Impurity Analysis", Site = Site.Macknade, IsRetired = false, MinTolerance = 0.2m, MaxTolerance = 0.5m },
        };
        await db.AnalysisTemplates.AddRangeAsync(templates, ct);
        await db.SaveChangesAsync(ct);

        var tpl = await db.AnalysisTemplates.OrderBy(t => t.Id).ToListAsync(ct);

        // Illustrative JSON config strings (BRD R1: tests, readings, calculations, validation rules).
        string PolConfig = "{\"tests\":[{\"id\":1,\"name\":\"Pol\",\"unit\":\"°Z\",\"method\":\"BSES\"},{\"id\":2,\"name\":\"Temperature\",\"unit\":\"°C\"}],\"sampleMethod\":\"Single (snap)\"}";
        string PolCalc = "{\"formula\":\"pol_corrected = pol * tempFactor(temperature)\",\"type\":\"calibration\"}";
        string PolValidation = "{\"rules\":[{\"field\":\"Pol\",\"min\":98.0,\"max\":99.8,\"type\":\"tolerance\"},{\"sequence\":[\"Brix\",\"Temperature\"]}]}";
        string BrixConfig = "{\"tests\":[{\"id\":3,\"name\":\"Brix\",\"unit\":\"°Bx\",\"method\":\"Refractometer\"}],\"sampleMethod\":\"Single (snap)\"}";
        string BrixCalc = "{\"formula\":\"brix = refractometer_reading\"}";
        string BrixValidation = "{\"rules\":[{\"field\":\"Brix\",\"min\":99.0,\"max\":99.9}]}";
        string MolValidation = "{\"rules\":[{\"crossField\":\"AMol > BMol > CMol\",\"type\":\"relationship\"},{\"field\":\"Purity\",\"min\":28,\"max\":40}]}";
        string PurityConfig = "{\"tests\":[{\"id\":5,\"name\":\"Purity\",\"unit\":\"%\"},{\"id\":6,\"name\":\"Pol\",\"unit\":\"°Z\"},{\"id\":7,\"name\":\"Brix\",\"unit\":\"°Bx\"}],\"sampleMethod\":\"Composite\"}";
        string PurityCalc = "{\"formula\":\"purity = pol / brix * 100\"}";

        var versions = new[]
        {
            // Inkerman versions
            new AnalysisTemplateVersion { TemplateId = tpl[0].Id, Version = 1, MinTolerance = 98.0m, MaxTolerance = 99.8m, TestConfiguration = PolConfig, CalculationDefinitions = PolCalc, ValidationRules = PolValidation, CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[1].Id, Version = 1, MinTolerance = 99.0m, MaxTolerance = 99.9m, TestConfiguration = BrixConfig, CalculationDefinitions = BrixCalc, ValidationRules = BrixValidation, CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[2].Id, Version = 1, MinTolerance = 0.0m, MaxTolerance = 0.15m, TestConfiguration = "{\"tests\":[{\"id\":4,\"name\":\"Water\",\"unit\":\"%\",\"method\":\"BSES\"}],\"sampleMethod\":\"Single (snap)\"}", CalculationDefinitions = "{\"formula\":\"moisture = (wet - dry) / wet * 100\",\"type\":\"weighted_average\"}", ValidationRules = "{\"rules\":[{\"field\":\"Water\",\"min\":0.0,\"max\":0.15}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[3].Id, Version = 1, MinTolerance = 100m, MaxTolerance = 1000m, TestConfiguration = "{\"tests\":[{\"id\":8,\"name\":\"ColourAICMS\",\"unit\":\"ICUMS\"}],\"sampleMethod\":\"Single (snap)\"}", CalculationDefinitions = "{\"formula\":\"colour = icms_reading\"}", ValidationRules = "{\"rules\":[{\"field\":\"ColourAICMS\",\"min\":100,\"max\":1000}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[4].Id, Version = 1, MinTolerance = 98.0m, MaxTolerance = 99.5m, TestConfiguration = PolConfig, CalculationDefinitions = PolCalc, ValidationRules = PolValidation, CreatedAtUtc = baseDate.AddYears(-1) },
            // Invicta versions
            new AnalysisTemplateVersion { TemplateId = tpl[5].Id, Version = 1, MinTolerance = 28.0m, MaxTolerance = 40.0m, TestConfiguration = PurityConfig, CalculationDefinitions = PurityCalc, ValidationRules = MolValidation, CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[6].Id, Version = 1, MinTolerance = 91.0m, MaxTolerance = 94.0m, TestConfiguration = BrixConfig, CalculationDefinitions = BrixCalc, ValidationRules = "{\"rules\":[{\"field\":\"Brix\",\"min\":91,\"max\":94}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[6].Id, Version = 2, MinTolerance = 90.5m, MaxTolerance = 94.5m, TestConfiguration = BrixConfig, CalculationDefinitions = BrixCalc, ValidationRules = "{\"rules\":[{\"field\":\"Brix\",\"min\":90.5,\"max\":94.5}]}", CreatedAtUtc = baseDate.AddDays(-7) },
            new AnalysisTemplateVersion { TemplateId = tpl[7].Id, Version = 1, MinTolerance = 85.0m, MaxTolerance = 88.0m, TestConfiguration = BrixConfig, CalculationDefinitions = BrixCalc, ValidationRules = "{\"rules\":[{\"field\":\"Brix\",\"min\":85,\"max\":88}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[8].Id, Version = 1, MinTolerance = 75.0m, MaxTolerance = 78.0m, TestConfiguration = BrixConfig, CalculationDefinitions = BrixCalc, ValidationRules = "{\"rules\":[{\"field\":\"Brix\",\"min\":75,\"max\":78}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[9].Id, Version = 1, MinTolerance = 82.0m, MaxTolerance = 88.0m, TestConfiguration = PurityConfig, CalculationDefinitions = PurityCalc, ValidationRules = "{\"rules\":[{\"field\":\"Purity\",\"min\":82,\"max\":88}]}", CreatedAtUtc = baseDate },
            // Kalamia versions
            new AnalysisTemplateVersion { TemplateId = tpl[10].Id, Version = 1, MinTolerance = 0.5m, MaxTolerance = 3.0m, TestConfiguration = PolConfig, CalculationDefinitions = "{}", ValidationRules = "{\"rules\":[{\"field\":\"Pol\",\"min\":0.5,\"max\":3.0}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[11].Id, Version = 1, MinTolerance = 1.5m, MaxTolerance = 3.5m, TestConfiguration = "{\"tests\":[{\"id\":9,\"name\":\"Ash\",\"unit\":\"%\"}],\"sampleMethod\":\"Single (snap)\"}", CalculationDefinitions = "{}", ValidationRules = "{\"rules\":[{\"field\":\"Ash\",\"min\":1.5,\"max\":3.5}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[12].Id, Version = 1, MinTolerance = 85.0m, MaxTolerance = 95.0m, TestConfiguration = "{\"tests\":[{\"id\":10,\"name\":\"PolRecovery\",\"unit\":\"%\"}],\"sampleMethod\":\"Composite\"}", CalculationDefinitions = "{}", ValidationRules = "{\"rules\":[{\"field\":\"PolRecovery\",\"min\":85,\"max\":95}]}", CreatedAtUtc = baseDate },
            // Pioneer versions
            new AnalysisTemplateVersion { TemplateId = tpl[13].Id, Version = 1, MinTolerance = 99.5m, MaxTolerance = 99.9m, TestConfiguration = PolConfig, CalculationDefinitions = PolCalc, ValidationRules = PolValidation, CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[14].Id, Version = 1, MinTolerance = 98.0m, MaxTolerance = 99.0m, TestConfiguration = BrixConfig, CalculationDefinitions = BrixCalc, ValidationRules = "{\"rules\":[{\"field\":\"Brix\",\"min\":98,\"max\":99}]}", CreatedAtUtc = baseDate },
            // Victoria versions
            new AnalysisTemplateVersion { TemplateId = tpl[15].Id, Version = 1, MinTolerance = 12.0m, MaxTolerance = 14.0m, TestConfiguration = PolConfig, CalculationDefinitions = PolCalc, ValidationRules = "{\"rules\":[{\"field\":\"Pol\",\"min\":12,\"max\":14}]}", CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[16].Id, Version = 1, MinTolerance = 10.0m, MaxTolerance = 14.0m, TestConfiguration = "{\"tests\":[{\"id\":11,\"name\":\"FibreContent\",\"unit\":\"%\"}],\"sampleMethod\":\"Composite\"}", CalculationDefinitions = "{}", ValidationRules = "{\"rules\":[{\"field\":\"FibreContent\",\"min\":10,\"max\":14}]}", CreatedAtUtc = baseDate },
            // Macknade versions
            new AnalysisTemplateVersion { TemplateId = tpl[17].Id, Version = 1, MinTolerance = 98.0m, MaxTolerance = 99.8m, TestConfiguration = PolConfig, CalculationDefinitions = PolCalc, ValidationRules = PolValidation, CreatedAtUtc = baseDate },
            new AnalysisTemplateVersion { TemplateId = tpl[18].Id, Version = 1, MinTolerance = 0.2m, MaxTolerance = 0.5m, TestConfiguration = "{\"tests\":[{\"id\":12,\"name\":\"Impurity\",\"unit\":\"%\"}],\"sampleMethod\":\"Single (snap)\"}", CalculationDefinitions = "{}", ValidationRules = "{\"rules\":[{\"field\":\"Impurity\",\"min\":0.2,\"max\":0.5}]}", CreatedAtUtc = baseDate },
        };
        await db.AnalysisTemplateVersions.AddRangeAsync(versions, ct);
        await db.SaveChangesAsync(ct);

        // Set CurrentVersionId for each template
        tpl[0].CurrentVersionId = versions[0].Id;
        tpl[1].CurrentVersionId = versions[1].Id;
        tpl[2].CurrentVersionId = versions[2].Id;
        tpl[3].CurrentVersionId = versions[3].Id;
        tpl[4].CurrentVersionId = versions[4].Id;
        tpl[5].CurrentVersionId = versions[5].Id;
        tpl[6].CurrentVersionId = versions[7].Id; // v2 is current
        tpl[7].CurrentVersionId = versions[8].Id;
        tpl[8].CurrentVersionId = versions[9].Id;
        tpl[9].CurrentVersionId = versions[10].Id;
        tpl[10].CurrentVersionId = versions[11].Id;
        tpl[11].CurrentVersionId = versions[12].Id;
        tpl[12].CurrentVersionId = versions[13].Id;
        tpl[13].CurrentVersionId = versions[14].Id;
        tpl[14].CurrentVersionId = versions[15].Id;
        tpl[15].CurrentVersionId = versions[16].Id;
        tpl[16].CurrentVersionId = versions[17].Id;
        tpl[17].CurrentVersionId = versions[18].Id;
        tpl[18].CurrentVersionId = versions[19].Id;
        db.AnalysisTemplates.UpdateRange(tpl);
        await db.SaveChangesAsync(ct);

        // ---- Schedules: 3-4 per seeded site, mix of active/inactive, assigned/unassigned ----
        var schedules = new[]
        {
            // Inkerman
            new Schedule { Name = "Sugar Pol - Every 2h (Day)", Site = Site.Inkerman, AnalysisType = "Sugar Pol (BSES)", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Every 2 hours during shift", ExclusionRules = "Not during scheduled maintenance", AssignedToUserId = InkAnalyst, IsActive = true },
            new Schedule { Name = "Sugar Brix - Hourly", Site = Site.Inkerman, AnalysisType = "Sugar Brix (Refractometer)", ShiftPattern = ShiftPattern.Shift, RecurrencePattern = "Hourly", ExclusionRules = null, AssignedToUserId = InkAnalyst, IsActive = true },
            new Schedule { Name = "Raw Sugar Colour - Weekly QA", Site = Site.Inkerman, AnalysisType = "Raw Sugar Colour", ShiftPattern = ShiftPattern.Weekly, RecurrencePattern = "Weekly on Friday", ExclusionRules = null, AssignedToUserId = null, IsActive = true },
            // Invicta
            new Schedule { Name = "Final Molasses - Per shift", Site = Site.Invicta, AnalysisType = "Final Molasses Purity", ShiftPattern = ShiftPattern.Shift, RecurrencePattern = "Once per shift", ExclusionRules = "Excludes Sundays", AssignedToUserId = InvAnalyst, IsActive = true },
            new Schedule { Name = "A Massecuite - Every 4h", Site = Site.Invicta, AnalysisType = "A Massecuite Brix", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Every 4 hours", ExclusionRules = null, AssignedToUserId = InvAnalyst, IsActive = true },
            new Schedule { Name = "Juice Purity - Daily", Site = Site.Invicta, AnalysisType = "Juice Purity", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Daily at 10:00", ExclusionRules = "Excludes weekends", AssignedToUserId = null, IsActive = true },
            // Kalamia
            new Schedule { Name = "Mud Pol - Day (suspended)", Site = Site.Kalamia, AnalysisType = "Mud Pol", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Every 4 hours", ExclusionRules = "Suspended during factory stoppage", AssignedToUserId = KalAnalyst, IsActive = false },
            new Schedule { Name = "Ash Content - Per shift", Site = Site.Kalamia, AnalysisType = "Ash Content", ShiftPattern = ShiftPattern.Shift, RecurrencePattern = "Once per shift", ExclusionRules = null, AssignedToUserId = KalAnalyst, IsActive = true },
            // Pioneer
            new Schedule { Name = "Final Sugar Pol - Hourly", Site = Site.Pioneer, AnalysisType = "Final Sugar Pol", ShiftPattern = ShiftPattern.Shift, RecurrencePattern = "Hourly", ExclusionRules = null, AssignedToUserId = PioAnalyst, IsActive = true },
            new Schedule { Name = "Final Sugar Brix - Every 2h", Site = Site.Pioneer, AnalysisType = "Final Sugar Brix", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Every 2 hours", ExclusionRules = null, AssignedToUserId = null, IsActive = true },
            // Victoria
            new Schedule { Name = "Cane Quality Pol - Daily", Site = Site.Victoria, AnalysisType = "Cane Quality Pol", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Daily at 06:00", ExclusionRules = null, AssignedToUserId = VicAnalyst, IsActive = true },
            new Schedule { Name = "Fibre Content - Weekly", Site = Site.Victoria, AnalysisType = "Plant Fibre Content", ShiftPattern = ShiftPattern.Weekly, RecurrencePattern = "Weekly on Tuesday", ExclusionRules = null, AssignedToUserId = null, IsActive = true },
            // Macknade
            new Schedule { Name = "Sugar Pol - Every 3h", Site = Site.Macknade, AnalysisType = "Sugar Pol (BSES)", ShiftPattern = ShiftPattern.Day, RecurrencePattern = "Every 3 hours", ExclusionRules = null, AssignedToUserId = MacAnalyst, IsActive = true },
            new Schedule { Name = "Impurity Analysis - Per shift", Site = Site.Macknade, AnalysisType = "Impurity Analysis", ShiftPattern = ShiftPattern.Shift, RecurrencePattern = "Once per shift", ExclusionRules = "Excludes maintenance days", AssignedToUserId = null, IsActive = true },
        };
        await db.Schedules.AddRangeAsync(schedules, ct);

        // ---- Samples: many across all sites and statuses (aim ~8-10 per site) ----
        Sample Smp(string ident, int templateId, LifecycleStatus status, Site site, Site? currentSite = null) =>
            new() { Identifier = ident, AnalysisTemplateId = templateId, Status = status, Site = site, CurrentSite = currentSite ?? site };
        var samples = new[]
        {
            // Inkerman (10)
            Smp("INK-2026-0001", tpl[0].Id, LifecycleStatus.InProgress, Site.Inkerman),
            Smp("INK-2026-0002", tpl[0].Id, LifecycleStatus.Completed, Site.Inkerman),
            Smp("INK-2026-0003", tpl[1].Id, LifecycleStatus.NotStarted, Site.Inkerman),
            Smp("INK-2026-0004", tpl[2].Id, LifecycleStatus.OnHold, Site.Inkerman),
            Smp("INK-2026-0005", tpl[3].Id, LifecycleStatus.Cancelled, Site.Inkerman),
            Smp("INK-2026-0006", tpl[0].Id, LifecycleStatus.Completed, Site.Inkerman),
            Smp("INK-2026-0007", tpl[1].Id, LifecycleStatus.InProgress, Site.Inkerman),
            Smp("INK-2026-0008", tpl[2].Id, LifecycleStatus.NotStarted, Site.Inkerman),
            Smp("INK-2026-0009", tpl[3].Id, LifecycleStatus.OnHold, Site.Inkerman),
            Smp("INK-2026-0010", tpl[0].Id, LifecycleStatus.Cancelled, Site.Inkerman),
            // Invicta (12 - for rich work queue)
            Smp("INV-2026-0001", tpl[5].Id, LifecycleStatus.InProgress, Site.Invicta),
            Smp("INV-2026-0002", tpl[6].Id, LifecycleStatus.Completed, Site.Invicta),
            Smp("INV-2026-0003", tpl[5].Id, LifecycleStatus.Cancelled, Site.Invicta),
            Smp("INV-2026-0004", tpl[6].Id, LifecycleStatus.NotStarted, Site.Invicta),
            Smp("INV-2026-0005", tpl[7].Id, LifecycleStatus.OnHold, Site.Invicta),
            Smp("INV-2026-0006", tpl[9].Id, LifecycleStatus.InProgress, Site.Invicta),
            Smp("INV-2026-0007", tpl[5].Id, LifecycleStatus.Completed, Site.Invicta),
            Smp("INV-2026-0008", tpl[8].Id, LifecycleStatus.NotStarted, Site.Invicta),
            Smp("INV-2026-0009", tpl[6].Id, LifecycleStatus.InProgress, Site.Invicta),
            Smp("INV-2026-0010", tpl[7].Id, LifecycleStatus.Completed, Site.Invicta),
            Smp("INV-2026-0011", tpl[9].Id, LifecycleStatus.OnHold, Site.Invicta),
            Smp("INV-2026-0012", tpl[5].Id, LifecycleStatus.NotStarted, Site.Invicta),
            // Kalamia (9)
            Smp("KAL-2026-0001", tpl[10].Id, LifecycleStatus.NotStarted, Site.Kalamia),
            Smp("KAL-2026-0002", tpl[11].Id, LifecycleStatus.Completed, Site.Kalamia),
            Smp("KAL-2026-0003", tpl[12].Id, LifecycleStatus.InProgress, Site.Kalamia),
            Smp("KAL-2026-0004", tpl[10].Id, LifecycleStatus.OnHold, Site.Kalamia),
            Smp("KAL-2026-0005", tpl[11].Id, LifecycleStatus.Cancelled, Site.Kalamia),
            Smp("KAL-2026-0006", tpl[12].Id, LifecycleStatus.NotStarted, Site.Kalamia),
            Smp("KAL-2026-0007", tpl[10].Id, LifecycleStatus.Completed, Site.Kalamia),
            Smp("KAL-2026-0008", tpl[11].Id, LifecycleStatus.InProgress, Site.Kalamia),
            Smp("KAL-2026-0009", tpl[12].Id, LifecycleStatus.OnHold, Site.Kalamia),
            // Pioneer (8)
            Smp("PIO-2026-0001", tpl[13].Id, LifecycleStatus.InProgress, Site.Pioneer),
            Smp("PIO-2026-0002", tpl[14].Id, LifecycleStatus.Completed, Site.Pioneer),
            Smp("PIO-2026-0003", tpl[13].Id, LifecycleStatus.NotStarted, Site.Pioneer),
            Smp("PIO-2026-0004", tpl[14].Id, LifecycleStatus.OnHold, Site.Pioneer),
            Smp("PIO-2026-0005", tpl[13].Id, LifecycleStatus.Cancelled, Site.Pioneer),
            Smp("PIO-2026-0006", tpl[14].Id, LifecycleStatus.Completed, Site.Pioneer),
            Smp("PIO-2026-0007", tpl[13].Id, LifecycleStatus.NotStarted, Site.Pioneer),
            Smp("PIO-2026-0008", tpl[14].Id, LifecycleStatus.InProgress, Site.Pioneer),
            // Victoria (9)
            Smp("VIC-2026-0001", tpl[15].Id, LifecycleStatus.InProgress, Site.Victoria),
            Smp("VIC-2026-0002", tpl[16].Id, LifecycleStatus.Completed, Site.Victoria),
            Smp("VIC-2026-0003", tpl[15].Id, LifecycleStatus.NotStarted, Site.Victoria),
            Smp("VIC-2026-0004", tpl[16].Id, LifecycleStatus.OnHold, Site.Victoria),
            Smp("VIC-2026-0005", tpl[15].Id, LifecycleStatus.Cancelled, Site.Victoria),
            Smp("VIC-2026-0006", tpl[16].Id, LifecycleStatus.Completed, Site.Victoria),
            Smp("VIC-2026-0007", tpl[15].Id, LifecycleStatus.InProgress, Site.Victoria),
            Smp("VIC-2026-0008", tpl[16].Id, LifecycleStatus.NotStarted, Site.Victoria),
            Smp("VIC-2026-0009", tpl[15].Id, LifecycleStatus.OnHold, Site.Victoria),
            // Macknade (10)
            Smp("MAC-2026-0001", tpl[17].Id, LifecycleStatus.InProgress, Site.Macknade),
            Smp("MAC-2026-0002", tpl[18].Id, LifecycleStatus.Completed, Site.Macknade),
            Smp("MAC-2026-0003", tpl[17].Id, LifecycleStatus.NotStarted, Site.Macknade),
            Smp("MAC-2026-0004", tpl[18].Id, LifecycleStatus.OnHold, Site.Macknade),
            Smp("MAC-2026-0005", tpl[17].Id, LifecycleStatus.Cancelled, Site.Macknade),
            Smp("MAC-2026-0006", tpl[18].Id, LifecycleStatus.Completed, Site.Macknade),
            Smp("MAC-2026-0007", tpl[17].Id, LifecycleStatus.InProgress, Site.Macknade),
            Smp("MAC-2026-0008", tpl[18].Id, LifecycleStatus.NotStarted, Site.Macknade),
            Smp("MAC-2026-0009", tpl[17].Id, LifecycleStatus.OnHold, Site.Macknade),
            Smp("MAC-2026-0010", tpl[18].Id, LifecycleStatus.Cancelled, Site.Macknade),
            // A few inter-site transfers
            Smp("INK-2026-0011", tpl[0].Id, LifecycleStatus.InProgress, Site.Inkerman, Site.Invicta),
            Smp("INV-2026-0013", tpl[5].Id, LifecycleStatus.Completed, Site.Invicta, Site.Kalamia),
        };
        await db.Samples.AddRangeAsync(samples, ct);
        await db.SaveChangesAsync(ct);

        var instList = await db.Instruments.OrderBy(i => i.Id).ToListAsync(ct);
        int PolInk = instList[0].Id, RefInk = instList[1].Id;
        int PolInv = instList[6].Id, RefInv = instList[7].Id;
        int PolKal = instList[11].Id;
        int PolPio = instList[15].Id;
        int PolVic = instList[19].Id;
        int PolMac = instList[23].Id;

        // ---- Analyses: aim 30+, across all 5 statuses, Invicta especially full ----
        Analysis An(int sampleId, int tplIdx, LifecycleStatus status, int startedBy, DateTimeOffset started,
            bool locked = false, DateTimeOffset? completed = null, int? lockedBy = null) =>
            new()
            {
                SampleId = sampleId, TemplateId = tpl[tplIdx].Id,
                // Always use the template's CURRENT version — robust regardless of how the
                // versions array is ordered (multi-version templates shift positional indices).
                TemplateVersionId = tpl[tplIdx].CurrentVersionId!.Value,
                Status = status, StartedAtUtc = started, StartedByUserId = startedBy,
                IsLocked = locked, CompletedAtUtc = completed,
                LockedAtUtc = locked ? completed : null, LockedByUserId = lockedBy,
            };

        var analyses = new[]
        {
            // Inkerman (6)
            An(samples[0].Id, 0, LifecycleStatus.InProgress, InkAnalyst, baseDate.AddHours(2)),
            An(samples[1].Id, 0, LifecycleStatus.Completed, InkAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(3), lockedBy: InkCoord),
            An(samples[3].Id, 2, LifecycleStatus.OnHold, InkAnalyst, baseDate.AddHours(1)),
            An(samples[5].Id, 1, LifecycleStatus.Completed, InkAnalyst, baseDate.AddDays(-2), locked: true, completed: baseDate.AddDays(-2).AddHours(2), lockedBy: InkCoord),
            An(samples[6].Id, 1, LifecycleStatus.InProgress, InkAnalyst, baseDate.AddHours(4)),
            An(samples[9].Id, 0, LifecycleStatus.Cancelled, InkAnalyst, baseDate.AddHours(3)),
            // Invicta (15 - full representation)
            An(samples[10].Id, 5, LifecycleStatus.InProgress, InvAnalyst, baseDate.AddHours(4)),
            An(samples[11].Id, 6, LifecycleStatus.Completed, InvAnalyst, baseDate.AddDays(-2), locked: true, completed: baseDate.AddDays(-2).AddHours(2), lockedBy: InvCoord),
            An(samples[12].Id, 5, LifecycleStatus.Cancelled, InvAnalyst, baseDate.AddDays(-1)),
            An(samples[13].Id, 6, LifecycleStatus.NotStarted, InvAnalyst, baseDate.AddHours(-1)),
            An(samples[14].Id, 7, LifecycleStatus.OnHold, InvAnalyst, baseDate.AddHours(1)),
            An(samples[15].Id, 9, LifecycleStatus.InProgress, InvAnalyst, baseDate.AddHours(5)),
            An(samples[16].Id, 5, LifecycleStatus.Completed, InvAnalyst, baseDate.AddDays(-3), locked: true, completed: baseDate.AddDays(-3).AddHours(1), lockedBy: InvCoord),
            An(samples[17].Id, 8, LifecycleStatus.NotStarted, InvAnalyst, baseDate.AddMinutes(30)),
            An(samples[18].Id, 6, LifecycleStatus.InProgress, InvAnalyst, baseDate.AddHours(3)),
            An(samples[19].Id, 7, LifecycleStatus.Completed, InvAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(4), lockedBy: InvCoord),
            An(samples[20].Id, 9, LifecycleStatus.OnHold, InvAnalyst, baseDate.AddHours(2)),
            An(samples[21].Id, 5, LifecycleStatus.NotStarted, InvAnalyst, baseDate),
            An(samples[31].Id, 5, LifecycleStatus.Completed, InvAnalyst, baseDate.AddDays(-4), locked: true, completed: baseDate.AddDays(-4).AddHours(3), lockedBy: InvCoord),
            // Kalamia (7)
            An(samples[22].Id, 10, LifecycleStatus.NotStarted, KalAnalyst, baseDate),
            An(samples[23].Id, 11, LifecycleStatus.Completed, KalAnalyst, baseDate.AddDays(-2), locked: true, completed: baseDate.AddDays(-2).AddHours(1), lockedBy: KalCoord),
            An(samples[24].Id, 12, LifecycleStatus.InProgress, KalAnalyst, baseDate.AddHours(2)),
            An(samples[25].Id, 10, LifecycleStatus.OnHold, KalAnalyst, baseDate.AddHours(1)),
            An(samples[27].Id, 12, LifecycleStatus.NotStarted, KalAnalyst, baseDate.AddHours(-2)),
            An(samples[28].Id, 10, LifecycleStatus.Completed, KalAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(2), lockedBy: KalCoord),
            An(samples[29].Id, 11, LifecycleStatus.InProgress, KalAnalyst, baseDate.AddHours(3)),
            // Pioneer (6)
            An(samples[32].Id, 13, LifecycleStatus.InProgress, PioAnalyst, baseDate.AddHours(2)),
            An(samples[33].Id, 14, LifecycleStatus.Completed, PioAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(3), lockedBy: PioCoord),
            An(samples[34].Id, 13, LifecycleStatus.NotStarted, PioAnalyst, baseDate),
            An(samples[35].Id, 14, LifecycleStatus.OnHold, PioAnalyst, baseDate.AddHours(1)),
            An(samples[37].Id, 14, LifecycleStatus.Completed, PioAnalyst, baseDate.AddDays(-2), locked: true, completed: baseDate.AddDays(-2).AddHours(1), lockedBy: PioCoord),
            An(samples[39].Id, 13, LifecycleStatus.InProgress, PioAnalyst, baseDate.AddHours(4)),
            // Victoria (7)
            An(samples[40].Id, 15, LifecycleStatus.InProgress, VicAnalyst, baseDate.AddHours(2)),
            An(samples[41].Id, 16, LifecycleStatus.Completed, VicAnalyst, baseDate.AddDays(-2), locked: true, completed: baseDate.AddDays(-2).AddHours(2), lockedBy: VicCoord),
            An(samples[42].Id, 15, LifecycleStatus.NotStarted, VicAnalyst, baseDate),
            An(samples[43].Id, 16, LifecycleStatus.OnHold, VicAnalyst, baseDate.AddHours(1)),
            An(samples[45].Id, 16, LifecycleStatus.Completed, VicAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(1), lockedBy: VicCoord),
            An(samples[46].Id, 15, LifecycleStatus.InProgress, VicAnalyst, baseDate.AddHours(3)),
            An(samples[48].Id, 16, LifecycleStatus.NotStarted, VicAnalyst, baseDate.AddMinutes(30)),
            // Macknade (7)
            An(samples[49].Id, 17, LifecycleStatus.InProgress, MacAnalyst, baseDate.AddHours(2)),
            An(samples[50].Id, 18, LifecycleStatus.Completed, MacAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(2), lockedBy: MacCoord),
            An(samples[51].Id, 17, LifecycleStatus.NotStarted, MacAnalyst, baseDate),
            An(samples[52].Id, 18, LifecycleStatus.OnHold, MacAnalyst, baseDate.AddHours(1)),
            An(samples[54].Id, 17, LifecycleStatus.Completed, MacAnalyst, baseDate.AddDays(-2), locked: true, completed: baseDate.AddDays(-2).AddHours(1), lockedBy: MacCoord),
            An(samples[56].Id, 18, LifecycleStatus.InProgress, MacAnalyst, baseDate.AddHours(3)),
            An(samples[59].Id, 17, LifecycleStatus.Cancelled, MacAnalyst, baseDate.AddHours(1)),
            // Inter-site transfers
            An(samples[58].Id, 0, LifecycleStatus.Completed, InkAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(1), lockedBy: InkCoord),
            An(samples[59].Id, 5, LifecycleStatus.Completed, InvAnalyst, baseDate.AddDays(-1), locked: true, completed: baseDate.AddDays(-1).AddHours(2), lockedBy: InvCoord),
            // Adherence demo: now-relative analyses so the adherence panel shows a live mix
            An(samples[10].Id, 5, LifecycleStatus.Completed, InvAnalyst, DateTimeOffset.UtcNow.AddMinutes(-30), locked: true, completed: DateTimeOffset.UtcNow.AddMinutes(-15), lockedBy: InvCoord),
        };
        await db.Analyses.AddRangeAsync(analyses, ct);
        await db.SaveChangesAsync(ct);

        // ---- Readings: 1-3 per non-NotStarted analysis ----
        Reading Rdg(int analysisId, int testId, decimal value, string unit, int by, int? instrument, string validation, decimal? calibrated, DateTimeOffset at) =>
            new() { AnalysisId = analysisId, TestId = testId, Value = value, Unit = unit, CapturedByUserId = by, InstrumentId = instrument, ValidationResult = validation, CalibratedValue = calibrated, CapturedAtUtc = at };
        var readings = new[]
        {
            // Inkerman analyses
            Rdg(analyses[0].Id, 1, 99.1m, "°Z", InkAnalyst, PolInk, "Valid", 99.0m, baseDate.AddHours(2).AddMinutes(10)),
            Rdg(analyses[0].Id, 2, 27.5m, "°C", InkAnalyst, null, "Valid", null, baseDate.AddHours(2).AddMinutes(12)),
            Rdg(analyses[0].Id, 1, 97.2m, "°Z", InkAnalyst, PolInk, "OutOfTolerance", 97.1m, baseDate.AddHours(2).AddMinutes(40)),
            Rdg(analyses[1].Id, 1, 99.4m, "°Z", InkAnalyst, PolInk, "Valid", 99.3m, baseDate.AddDays(-1).AddHours(1)),
            Rdg(analyses[1].Id, 2, 26.0m, "°C", InkAnalyst, null, "Valid", null, baseDate.AddDays(-1).AddHours(1)),
            Rdg(analyses[2].Id, 4, 0.12m, "%", InkAnalyst, null, "Valid", null, baseDate.AddHours(1).AddMinutes(5)),
            Rdg(analyses[3].Id, 3, 99.2m, "°Bx", InkAnalyst, RefInk, "Valid", null, baseDate.AddDays(-2).AddHours(2)),
            Rdg(analyses[4].Id, 3, 99.3m, "°Bx", InkAnalyst, RefInk, "Valid", null, baseDate.AddHours(4).AddMinutes(10)),
            Rdg(analyses[5].Id, 1, 98.5m, "°Z", InkAnalyst, PolInk, "Valid", 98.4m, baseDate.AddHours(3).AddMinutes(5)),
            // Invicta analyses (many readings for full representation)
            Rdg(analyses[6].Id, 5, 32.5m, "%", InvAnalyst, PolInv, "Valid", null, baseDate.AddHours(4).AddMinutes(10)),
            Rdg(analyses[6].Id, 6, 87.3m, "°Z", InvAnalyst, PolInv, "Valid", 87.2m, baseDate.AddHours(4).AddMinutes(15)),
            Rdg(analyses[6].Id, 7, 91.2m, "°Bx", InvAnalyst, RefInv, "OutOfTolerance", null, baseDate.AddHours(4).AddMinutes(20)),
            Rdg(analyses[7].Id, 3, 92.5m, "°Bx", InvAnalyst, RefInv, "Valid", null, baseDate.AddDays(-2).AddHours(1)),
            Rdg(analyses[8].Id, 5, 25.0m, "%", InvAnalyst, PolInv, "OutOfTolerance", null, baseDate.AddDays(-1).AddHours(2)),
            Rdg(analyses[10].Id, 3, 87.5m, "°Bx", InvAnalyst, RefInv, "Valid", null, baseDate.AddHours(1).AddMinutes(10)),
            Rdg(analyses[11].Id, 5, 35.2m, "%", InvAnalyst, PolInv, "Valid", null, baseDate.AddHours(5).AddMinutes(5)),
            Rdg(analyses[12].Id, 3, 91.8m, "°Bx", InvAnalyst, RefInv, "Valid", null, baseDate.AddDays(-3).AddHours(3)),
            Rdg(analyses[13].Id, 3, 85.5m, "°Bx", InvAnalyst, RefInv, "Valid", null, baseDate.AddHours(3).AddMinutes(15)),
            Rdg(analyses[14].Id, 3, 76.2m, "°Bx", InvAnalyst, RefInv, "Valid", null, baseDate.AddHours(2).AddMinutes(10)),
            Rdg(analyses[15].Id, 5, 34.0m, "%", InvAnalyst, PolInv, "Valid", null, baseDate.AddHours(5).AddMinutes(20)),
            Rdg(analyses[16].Id, 7, 88.5m, "°Bx", InvAnalyst, RefInv, "Valid", null, baseDate.AddDays(-1).AddHours(4)),
            Rdg(analyses[18].Id, 3, 92.1m, "°Bx", InvAnalyst, RefInv, "Valid", null, baseDate.AddHours(3).AddMinutes(5)),
            Rdg(analyses[19].Id, 3, 86.9m, "°Bx", InvAnalyst, RefInv, "Valid", null, baseDate.AddDays(-1).AddHours(1)),
            Rdg(analyses[20].Id, 5, 31.5m, "%", InvAnalyst, PolInv, "OutOfTolerance", null, baseDate.AddHours(2).AddMinutes(15)),
            Rdg(analyses[38].Id, 5, 33.7m, "%", InvAnalyst, PolInv, "Valid", null, baseDate.AddDays(-4).AddHours(2)),
            // Kalamia analyses
            Rdg(analyses[21].Id, 9, 2.3m, "%", KalAnalyst, null, "Valid", null, baseDate.AddMinutes(30)),
            Rdg(analyses[22].Id, 9, 2.1m, "%", KalAnalyst, null, "Valid", null, baseDate.AddDays(-2).AddHours(1)),
            Rdg(analyses[23].Id, 10, 89.5m, "%", KalAnalyst, PolKal, "Valid", 89.3m, baseDate.AddHours(2).AddMinutes(10)),
            Rdg(analyses[24].Id, 1, 1.8m, "°Z", KalAnalyst, PolKal, "Valid", 1.7m, baseDate.AddHours(1).AddMinutes(5)),
            Rdg(analyses[26].Id, 10, 87.2m, "%", KalAnalyst, PolKal, "Valid", 87.0m, baseDate.AddDays(-1).AddHours(2)),
            Rdg(analyses[27].Id, 9, 2.5m, "%", KalAnalyst, null, "Valid", null, baseDate.AddHours(3).AddMinutes(10)),
            // Pioneer analyses
            Rdg(analyses[28].Id, 1, 99.7m, "°Z", PioAnalyst, PolPio, "Valid", 99.6m, baseDate.AddHours(2).AddMinutes(10)),
            Rdg(analyses[28].Id, 2, 24.5m, "°C", PioAnalyst, null, "Valid", null, baseDate.AddHours(2).AddMinutes(12)),
            Rdg(analyses[29].Id, 3, 98.8m, "°Bx", PioAnalyst, null, "Valid", null, baseDate.AddDays(-1).AddHours(1)),
            Rdg(analyses[30].Id, 1, 99.5m, "°Z", PioAnalyst, PolPio, "Valid", 99.4m, baseDate.AddMinutes(30)),
            Rdg(analyses[31].Id, 3, 98.5m, "°Bx", PioAnalyst, null, "Valid", null, baseDate.AddHours(1).AddMinutes(5)),
            Rdg(analyses[32].Id, 3, 98.2m, "°Bx", PioAnalyst, null, "Valid", null, baseDate.AddDays(-2).AddHours(1)),
            Rdg(analyses[33].Id, 1, 99.8m, "°Z", PioAnalyst, PolPio, "Valid", 99.7m, baseDate.AddHours(4).AddMinutes(15)),
            // Victoria analyses
            Rdg(analyses[34].Id, 1, 13.2m, "°Z", VicAnalyst, PolVic, "Valid", 13.1m, baseDate.AddHours(2).AddMinutes(10)),
            Rdg(analyses[35].Id, 11, 11.8m, "%", VicAnalyst, null, "Valid", null, baseDate.AddDays(-2).AddHours(1)),
            Rdg(analyses[36].Id, 1, 12.5m, "°Z", VicAnalyst, PolVic, "Valid", 12.4m, baseDate),
            Rdg(analyses[37].Id, 11, 12.2m, "%", VicAnalyst, null, "Valid", null, baseDate.AddHours(1).AddMinutes(5)),
            Rdg(analyses[38].Id, 1, 13.1m, "°Z", VicAnalyst, PolVic, "Valid", 13.0m, baseDate.AddDays(-1).AddHours(1)),
            Rdg(analyses[39].Id, 11, 11.5m, "%", VicAnalyst, null, "OutOfTolerance", null, baseDate.AddHours(3).AddMinutes(15)),
            Rdg(analyses[40].Id, 1, 12.8m, "°Z", VicAnalyst, PolVic, "Valid", 12.7m, baseDate.AddMinutes(30)),
            // Macknade analyses
            Rdg(analyses[41].Id, 1, 98.9m, "°Z", MacAnalyst, PolMac, "Valid", 98.8m, baseDate.AddHours(2).AddMinutes(10)),
            Rdg(analyses[42].Id, 12, 0.35m, "%", MacAnalyst, null, "Valid", null, baseDate.AddDays(-1).AddHours(1)),
            Rdg(analyses[43].Id, 1, 98.6m, "°Z", MacAnalyst, PolMac, "Valid", 98.5m, baseDate),
            Rdg(analyses[44].Id, 12, 0.38m, "%", MacAnalyst, null, "Valid", null, baseDate.AddHours(1).AddMinutes(5)),
            Rdg(analyses[45].Id, 1, 99.1m, "°Z", MacAnalyst, PolMac, "Valid", 99.0m, baseDate.AddDays(-2).AddHours(1)),
            Rdg(analyses[46].Id, 12, 0.42m, "%", MacAnalyst, null, "OutOfTolerance", null, baseDate.AddHours(3).AddMinutes(10)),
            Rdg(analyses[47].Id, 1, 97.8m, "°Z", MacAnalyst, PolMac, "OutOfTolerance", 97.7m, baseDate.AddHours(1).AddMinutes(5)),
            // Inter-site transfers
            Rdg(analyses[46].Id, 1, 99.2m, "°Z", InkAnalyst, PolInk, "Valid", 99.1m, baseDate.AddDays(-1).AddHours(1)),
            Rdg(analyses[47].Id, 5, 34.5m, "%", InvAnalyst, PolInv, "Valid", null, baseDate.AddDays(-1).AddHours(2)),
        };
        await db.Readings.AddRangeAsync(readings, ct);
        await db.SaveChangesAsync(ct);

        var rdgList = await db.Readings.OrderBy(r => r.Id).ToListAsync(ct);
        int outOfTolInk = rdgList[2].Id;     // the 97.2 Pol reading
        int outOfTolInv1 = rdgList[12].Id;   // the 91.2 Brix (out of tolerance for Molasses Purity)
        int outOfTolInv2 = rdgList[14].Id;   // the 25.0 purity reading
        int outOfTolInv3 = rdgList[20].Id;   // the 31.5 purity reading
        int outOfTolVic = rdgList[39].Id;    // the 11.5 fibre content
        int outOfTolMac1 = rdgList[46].Id;   // the 0.42 impurity
        int outOfTolMac2 = rdgList[47].Id;   // the 97.8 Pol

        // ---- ExceptionRecords: mix of open and resolved ----
        var exceptions = new[]
        {
            new ExceptionRecord { AnalysisId = analyses[0].Id, ReadingId = outOfTolInk, Reason = "Pol 97.2 °Z below expected range 98.0-99.8", Decision = null },
            new ExceptionRecord { AnalysisId = analyses[6].Id, ReadingId = outOfTolInv1, Reason = "A Massecuite Brix 91.2 °Bx out of spec 91.0-94.0", Decision = "AcceptWithComment", DecisionComment = "Borderline reading; confirmed by second operator; approved for batch.", DecidedByUserId = InvCoord, DecidedAtUtc = baseDate.AddHours(5) },
            new ExceptionRecord { AnalysisId = analyses[8].Id, ReadingId = outOfTolInv2, Reason = "Final molasses purity 25.0% below minimum 28.0%", Decision = "AcceptWithComment", DecisionComment = "Confirmed low-purity C molasses batch; accepted per shift coordinator.", DecidedByUserId = InvCoord, DecidedAtUtc = baseDate.AddDays(-1).AddHours(3) },
            new ExceptionRecord { AnalysisId = analyses[20].Id, ReadingId = outOfTolInv3, Reason = "Final molasses purity 31.5% below target; review required", Decision = null },
            new ExceptionRecord { AnalysisId = analyses[39].Id, ReadingId = outOfTolVic, Reason = "Plant Fibre Content 11.5% below minimum 12.0%", Decision = "Reject", DecisionComment = "Inconsistent with harvest records; sample compromised; rejected.", DecidedByUserId = VicCoord, DecidedAtUtc = baseDate.AddHours(4) },
            new ExceptionRecord { AnalysisId = analyses[46].Id, ReadingId = outOfTolMac1, Reason = "Impurity 0.42% exceeds maximum 0.5%", Decision = null },
            new ExceptionRecord { AnalysisId = analyses[47].Id, ReadingId = outOfTolMac2, Reason = "Sugar Pol 97.8 °Z below spec 98.0-99.8", Decision = "AcceptWithComment", DecisionComment = "Instrument calibration drift detected; recalibrated and rerun scheduled.", DecidedByUserId = MacCoord, DecidedAtUtc = baseDate.AddHours(2) },
            new ExceptionRecord { AnalysisId = analyses[1].Id, ReadingId = rdgList[4].Id, Reason = "Temperature measurement anomaly", Decision = "AcceptWithComment", DecisionComment = "HVAC malfunction during test; re-run confirms results.", DecidedByUserId = InkCoord, DecidedAtUtc = baseDate.AddDays(-1).AddHours(4) },
        };
        await db.ExceptionRecords.AddRangeAsync(exceptions, ct);
        await db.SaveChangesAsync(ct);

        // ---- Calibration curves (linear standards) ----
        var curves = new[]
        {
            new CalibrationCurve { Name = "Polarimeter Standard - Inkerman", AnalysisTemplateId = tpl[0].Id, IsActive = true },
            new CalibrationCurve { Name = "Refractometer Standard - Inkerman", AnalysisTemplateId = tpl[1].Id, IsActive = true },
            new CalibrationCurve { Name = "Polarimeter Standard - Invicta", AnalysisTemplateId = tpl[5].Id, IsActive = true },
            new CalibrationCurve { Name = "Brix Standard - Invicta", AnalysisTemplateId = tpl[6].Id, IsActive = true },
            new CalibrationCurve { Name = "Polarimeter Standard - Kalamia", AnalysisTemplateId = tpl[10].Id, IsActive = true },
            new CalibrationCurve { Name = "Polarimeter Standard - Pioneer", AnalysisTemplateId = tpl[13].Id, IsActive = true },
            new CalibrationCurve { Name = "Polarimeter Standard - Victoria", AnalysisTemplateId = tpl[15].Id, IsActive = true },
            new CalibrationCurve { Name = "Polarimeter Standard - Macknade", AnalysisTemplateId = tpl[17].Id, IsActive = true },
        };
        await db.CalibrationCurves.AddRangeAsync(curves, ct);
        await db.SaveChangesAsync(ct);

        var curveList = await db.CalibrationCurves.OrderBy(c => c.Id).ToListAsync(ct);
        var points = new[]
        {
            // Polarimeter Standard - Inkerman
            new CalibrationPoint { CalibrationCurveId = curveList[0].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[0].Id, XValue = 50m, YValue = 49.9m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[0].Id, XValue = 100m, YValue = 99.8m, Order = 2 },
            // Refractometer Standard - Inkerman
            new CalibrationPoint { CalibrationCurveId = curveList[1].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[1].Id, XValue = 50m, YValue = 50.5m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[1].Id, XValue = 100m, YValue = 100m, Order = 2 },
            // Polarimeter Standard - Invicta
            new CalibrationPoint { CalibrationCurveId = curveList[2].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[2].Id, XValue = 50m, YValue = 51m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[2].Id, XValue = 100m, YValue = 100m, Order = 2 },
            // Brix Standard - Invicta
            new CalibrationPoint { CalibrationCurveId = curveList[3].Id, XValue = 0m, YValue = 0.1m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[3].Id, XValue = 50m, YValue = 50.2m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[3].Id, XValue = 100m, YValue = 100.1m, Order = 2 },
            // Polarimeter Standard - Kalamia
            new CalibrationPoint { CalibrationCurveId = curveList[4].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[4].Id, XValue = 50m, YValue = 49.8m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[4].Id, XValue = 100m, YValue = 99.7m, Order = 2 },
            // Polarimeter Standard - Pioneer
            new CalibrationPoint { CalibrationCurveId = curveList[5].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[5].Id, XValue = 50m, YValue = 50m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[5].Id, XValue = 100m, YValue = 100m, Order = 2 },
            // Polarimeter Standard - Victoria
            new CalibrationPoint { CalibrationCurveId = curveList[6].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[6].Id, XValue = 50m, YValue = 49.9m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[6].Id, XValue = 100m, YValue = 99.9m, Order = 2 },
            // Polarimeter Standard - Macknade
            new CalibrationPoint { CalibrationCurveId = curveList[7].Id, XValue = 0m, YValue = 0m, Order = 0 },
            new CalibrationPoint { CalibrationCurveId = curveList[7].Id, XValue = 50m, YValue = 49.95m, Order = 1 },
            new CalibrationPoint { CalibrationCurveId = curveList[7].Id, XValue = 100m, YValue = 99.85m, Order = 2 },
        };
        await db.CalibrationPoints.AddRangeAsync(points, ct);
        await db.SaveChangesAsync(ct);

        // ---- SampleTransfers: 4-6 transfers between sites ----
        var sampleList = await db.Samples.OrderBy(s => s.Id).ToListAsync(ct);
        var transfers = new[]
        {
            new SampleTransfer { SampleId = sampleList[58].Id, FromSite = Site.Inkerman, ToSite = Site.Invicta, TransferredByUserId = InkCoord, TransferredAtUtc = baseDate.AddDays(-2) },
            new SampleTransfer { SampleId = sampleList[59].Id, FromSite = Site.Invicta, ToSite = Site.Kalamia, TransferredByUserId = InvCoord, TransferredAtUtc = baseDate.AddDays(-1).AddHours(2) },
            new SampleTransfer { SampleId = sampleList[24].Id, FromSite = Site.Kalamia, ToSite = Site.Pioneer, TransferredByUserId = KalCoord, TransferredAtUtc = baseDate.AddHours(1) },
            new SampleTransfer { SampleId = sampleList[33].Id, FromSite = Site.Pioneer, ToSite = Site.Victoria, TransferredByUserId = PioCoord, TransferredAtUtc = baseDate.AddDays(-1).AddHours(3) },
            new SampleTransfer { SampleId = sampleList[45].Id, FromSite = Site.Victoria, ToSite = Site.Macknade, TransferredByUserId = VicCoord, TransferredAtUtc = baseDate.AddMinutes(30) },
            new SampleTransfer { SampleId = sampleList[51].Id, FromSite = Site.Macknade, ToSite = Site.Inkerman, TransferredByUserId = MacCoord, TransferredAtUtc = baseDate.AddDays(-1) },
        };
        await db.SampleTransfers.AddRangeAsync(transfers, ct);

        // ---- IntegrationLogs: handful of illustrative entries ----
        var integrationLogs = new[]
        {
            new IntegrationLogEntry { TargetSystem = "Databank", AnalysisId = analyses[1].Id, Status = "Success", AttemptedAtUtc = baseDate.AddDays(-1).AddHours(4), CompletedAtUtc = baseDate.AddDays(-1).AddHours(4).AddMinutes(30), ErrorMessage = null, RetryCount = 0 },
            new IntegrationLogEntry { TargetSystem = "SCADA", AnalysisId = analyses[7].Id, Status = "Success", AttemptedAtUtc = baseDate.AddDays(-2).AddHours(2), CompletedAtUtc = baseDate.AddDays(-2).AddHours(2).AddMinutes(15), ErrorMessage = null, RetryCount = 0 },
            new IntegrationLogEntry { TargetSystem = "DataLakehouse", AnalysisId = analyses[16].Id, Status = "Success", AttemptedAtUtc = baseDate.AddDays(-3).AddHours(1), CompletedAtUtc = baseDate.AddDays(-3).AddHours(1).AddMinutes(45), ErrorMessage = null, RetryCount = 1 },
            new IntegrationLogEntry { TargetSystem = "Databank", AnalysisId = analyses[22].Id, Status = "Pending", AttemptedAtUtc = baseDate.AddHours(1), CompletedAtUtc = null, ErrorMessage = null, RetryCount = 0 },
            new IntegrationLogEntry { TargetSystem = "SCADA", AnalysisId = analyses[29].Id, Status = "Failed", AttemptedAtUtc = baseDate.AddDays(-1).AddHours(3), CompletedAtUtc = null, ErrorMessage = "Connection timeout after 30s", RetryCount = 2 },
        };
        await db.IntegrationLogs.AddRangeAsync(integrationLogs, ct);

        // ---- AuditLogs: illustrative recorded history so the audit trail viewer has content (R3) ----
        const string analystRole = "ControlLabAnalyst";
        const string coordRole = "LabCoordinator";
        var auditLogs = new[]
        {
            new AuditLogEntry { UserId = InkAnalyst, Role = analystRole, TimestampUtc = baseDate.AddDays(-2).AddHours(1).AddMinutes(10), Action = "ReadingCaptured", EntityType = "Reading", EntityId = analyses[1].Id, AfterValues = "Value: 99.4, Status: Valid" },
            new AuditLogEntry { UserId = InkAnalyst, Role = analystRole, TimestampUtc = baseDate.AddDays(-2).AddHours(1).AddMinutes(40), Action = "ExceptionCreated", EntityType = "ExceptionRecord", EntityId = analyses[3].Id, AfterValues = "Reason: Reading 97.2 is below minimum tolerance of 98.0." },
            new AuditLogEntry { UserId = InkCoord, Role = coordRole, TimestampUtc = baseDate.AddDays(-2).AddHours(2), Action = "ExceptionDecided", EntityType = "ExceptionRecord", EntityId = analyses[3].Id, BeforeValues = "Decision: , Comment: ", AfterValues = "Decision: AcceptWithComment, Comment: Confirmed against retained sample." },
            new AuditLogEntry { UserId = InkAnalyst, Role = analystRole, TimestampUtc = baseDate.AddDays(-1).AddHours(3), Action = "StatusChanged", EntityType = "Analysis", EntityId = analyses[1].Id, BeforeValues = "Status: InProgress", AfterValues = "Status: Completed, IsLocked: True" },
            new AuditLogEntry { UserId = InkCoord, Role = coordRole, TimestampUtc = baseDate.AddDays(-1).AddHours(5), Action = "ResultUnlocked", EntityType = "Analysis", EntityId = analyses[3].Id, BeforeValues = "IsLocked: True", AfterValues = "IsLocked: False, Justification: Re-test required after instrument recalibration." },
            new AuditLogEntry { UserId = InvCoord, Role = coordRole, TimestampUtc = baseDate.AddDays(-1).AddHours(1), Action = "CalibrationCurveCreated", EntityType = "CalibrationCurve", EntityId = curveList[2].Id, AfterValues = "Name: Polarimeter Standard - Invicta, Points: 3" },
            new AuditLogEntry { UserId = InvCoord, Role = coordRole, TimestampUtc = baseDate.AddDays(-1).AddHours(6), Action = "TemplateUpdated", EntityType = "AnalysisTemplate", EntityId = tpl[6].Id, BeforeValues = "Version: 1", AfterValues = "Version: 2" },
            new AuditLogEntry { UserId = KalAnalyst, Role = analystRole, TimestampUtc = baseDate.AddHours(-4), Action = "ReadingCaptured", EntityType = "Reading", EntityId = analyses[10].Id, AfterValues = "Value: 2.1, Status: Valid" },
            new AuditLogEntry { UserId = PioCoord, Role = coordRole, TimestampUtc = baseDate.AddHours(-2), Action = "CalibrationCurveDeactivated", EntityType = "CalibrationCurve", EntityId = curveList[5].Id, AfterValues = "IsActive: false" },
            new AuditLogEntry { UserId = VicAnalyst, Role = analystRole, TimestampUtc = baseDate.AddMinutes(-90), Action = "StatusChanged", EntityType = "Analysis", EntityId = analyses[15].Id, BeforeValues = "Status: NotStarted", AfterValues = "Status: InProgress" },
            new AuditLogEntry { UserId = MacCoord, Role = coordRole, TimestampUtc = baseDate.AddMinutes(-30), Action = "SampleTransferred", EntityType = "Sample", EntityId = sampleList[51].Id, AfterValues = "FromSite: Macknade, ToSite: Inkerman" },
            new AuditLogEntry { UserId = InkCoord, Role = coordRole, TimestampUtc = baseDate.AddMinutes(-5), Action = "ReadingCaptured", EntityType = "Reading", EntityId = analyses[0].Id, AfterValues = "Value: 99.1, Status: Valid" },
        };
        await db.AuditLogs.AddRangeAsync(auditLogs, ct);

        await db.SaveChangesAsync(ct);
    }
}
