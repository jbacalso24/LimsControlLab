namespace LimsControlLab.SharedKernel.Enums;

/// <summary>
/// Shift scheduling pattern (R10, R11).
/// Fixed 3×8-hour shift model aligned to Databank: 08:00-16:00, 16:00-00:00, 00:00-08:00.
/// </summary>
public enum ShiftPattern
{
    Day = 1,
    Shift = 2,
    Weekly = 3,
}
