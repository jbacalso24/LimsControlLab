namespace LimsControlLab.SharedKernel.Enums;

/// <summary>
/// Laboratory roles in LIMS Control Lab.
/// This is the single canonical definition; every authorization check references it.
/// </summary>
public enum Role
{
    ControlLabAnalyst = 1,
    LabCoordinator = 2,
}
