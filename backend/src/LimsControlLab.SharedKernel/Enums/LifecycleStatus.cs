namespace LimsControlLab.SharedKernel.Enums;

/// <summary>
/// The lifecycle status of a sample or analysis.
/// This is the single canonical definition; every status transition references it.
/// </summary>
public enum LifecycleStatus
{
    NotStarted = 1,
    InProgress = 2,
    OnHold = 3,
    Completed = 4,
    Cancelled = 5,
}
