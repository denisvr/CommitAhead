using Devalente.Shared.AspNetCore.Security.Testing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CommitAhead.Api.Tests.Security;

/// <summary>
/// Mechanical proof that every MVC action declares its authorization explicitly and that the set of
/// anonymous endpoints matches <see cref="ApprovedAnonymousEndpoints"/> exactly. The fallback policy
/// (AddCommitAheadAuthentication) already denies by default, so this is not the enforcement point —
/// it exists because a missing declaration is invisible in review, and because an anonymous endpoint
/// added without review is exactly the change nobody notices.
///
/// It runs against a real ASP.NET Core test host so the controller and action metadata are the
/// deployable application's, not a reflection approximation. A plain WebApplicationFactory is enough:
/// no request is sent, so no database or Supabase stub is needed.
/// </summary>
public class EndpointAuthorizationInventoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointAuthorizationInventoryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void EveryEndpoint_DeclaresAuthorization_AndAnonymousAccessMatchesTheApprovedInventory()
    {
        var actionDescriptors = _factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>();

        var report = MvcEndpointAuthorizationVerifier.Verify(actionDescriptors, ApprovedAnonymousEndpoints.All);

        Assert.True(report.IsValid, report.ToString());
    }

    [Fact]
    public void TheInventoryIsNotEmpty_SoAPassingReportCannotMeanNoEndpointsWereInspected()
    {
        var actionDescriptors = _factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>();

        var endpoints = MvcEndpointAuthorizationVerifier.Inspect(actionDescriptors);

        Assert.NotEmpty(endpoints);
        Assert.Contains(endpoints, endpoint => endpoint.Authorization == EndpointAuthorizationKind.Authorized);
    }
}
