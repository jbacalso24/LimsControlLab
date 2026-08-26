using LimsControlLab.Api.Auth;
using LimsControlLab.SharedKernel.Enums;
using Xunit;

namespace LimsControlLab.Api.Tests.Auth;

public sealed class SiteRoleRequirementTests
{
    [Fact]
    public void RequirementWithRoleStoresRoleCorrectly()
    {
        var requirement = new SiteRoleRequirement(requiredRole: Role.ControlLabAnalyst);

        Assert.Equal(Role.ControlLabAnalyst, requirement.RequiredRole);
        Assert.Null(requirement.RequiredSite);
    }

    [Fact]
    public void RequirementWithSiteStoresSiteCorrectly()
    {
        var requirement = new SiteRoleRequirement(requiredSite: Site.Inkerman);

        Assert.Null(requirement.RequiredRole);
        Assert.Equal(Site.Inkerman, requirement.RequiredSite);
    }

    [Fact]
    public void RequirementWithBothStoresBoth()
    {
        var requirement = new SiteRoleRequirement(requiredRole: Role.LabCoordinator, requiredSite: Site.Victoria);

        Assert.Equal(Role.LabCoordinator, requirement.RequiredRole);
        Assert.Equal(Site.Victoria, requirement.RequiredSite);
    }
}
