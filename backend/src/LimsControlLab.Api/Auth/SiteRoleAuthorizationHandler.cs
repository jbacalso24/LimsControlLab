using LimsControlLab.SharedKernel.Enums;
using Microsoft.AspNetCore.Authorization;

namespace LimsControlLab.Api.Auth;

/// <summary>
/// Enforces role + site authorization checks via custom policies.
/// Policies are named: "Role.<RoleName>" for role-based access, "Role.<RoleName>.Site.<SiteName>" for site+role.
/// </summary>
public sealed class SiteRoleAuthorizationHandler : AuthorizationHandler<SiteRoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SiteRoleRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var siteClaim = context.User.FindFirst("site")?.Value;

        if (string.IsNullOrEmpty(roleClaim) || string.IsNullOrEmpty(siteClaim))
            return Task.CompletedTask;

        if (!Enum.TryParse<Role>(roleClaim, out var userRole))
            return Task.CompletedTask;

        if (!Enum.TryParse<Site>(siteClaim, out var userSite))
            return Task.CompletedTask;

        if (requirement.RequiredRole.HasValue && userRole != requirement.RequiredRole.Value)
            return Task.CompletedTask;

        if (requirement.RequiredSite.HasValue && userSite != requirement.RequiredSite.Value)
            return Task.CompletedTask;

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorization requirement for role + site checks.
/// </summary>
public sealed class SiteRoleRequirement : IAuthorizationRequirement
{
    public Role? RequiredRole { get; }
    public Site? RequiredSite { get; }

    public SiteRoleRequirement(Role? requiredRole = null, Site? requiredSite = null)
    {
        RequiredRole = requiredRole;
        RequiredSite = requiredSite;
    }
}

/// <summary>
/// Authorization policy definitions for role + site combinations.
/// Name format: "Role.Analyst" for role-only, "Role.Analyst.Site.Inkerman" for site+role.
/// </summary>
public static class AuthorizationPolicies
{
    public const string AnalystRead = "Role.ControlLabAnalyst";
    public const string CoordinatorWrite = "Role.LabCoordinator";

    public static void AddSiteRolePolicies(AuthorizationOptions options)
    {
        foreach (var role in Enum.GetValues<Role>())
        {
            var policyName = $"Role.{role}";
            options.AddPolicy(policyName, policy =>
                policy.AddRequirements(new SiteRoleRequirement(requiredRole: role)));
        }
    }
}
