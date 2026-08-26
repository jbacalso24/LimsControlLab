namespace LimsControlLab.SharedKernel;

/// <summary>
/// Convention for labelling illustrative data or honest stubs in the codebase.
/// Task 1 uses this for seed/fixture data marked as not production-ready.
/// </summary>
public enum FidelityLabel
{
    /// <summary>Real production data or logic.</summary>
    Production = 1,

    /// <summary>Illustrative seed data or a stub implementation pending real logic.</summary>
    Illustrative = 2,
}
