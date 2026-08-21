using CommitAhead.Api.Security;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CommitAhead.Api.Tests.Security;

/// <summary>
/// Asserts that the finite transport ceilings are actually configured on the host.
///
/// These are deliberately configuration assertions, not enforcement assertions. A
/// WebApplicationFactory host does not run Kestrel, so no in-process request can be rejected by
/// Kestrel's body limit — claiming otherwise would be the kind of false green these standards
/// explicitly warn about. Enforcement of MaxRequestBodySize is a deployed check, recorded as such
/// in docs/security/threat-model.md ("Evidence register"). The JSON depth limits, by contrast, are
/// enforced by the serializer and so hold in process too; what is asserted here is that both
/// serializer configurations carry the value, because the API has two (MVC's and Http.Json's, which
/// the OpenAPI generator reads) and they must not drift apart.
/// </summary>
public class TransportLimitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransportLimitTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void KestrelBodyLimit_IsFinite_AndFarBelowTheFrameworkDefault()
    {
        var options = _factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        Assert.Equal(TransportLimits.MaxRequestBodyBytes, options.Limits.MaxRequestBodySize);

        // Guards the derivation itself, not just the assignment: the cap has to stay above the
        // largest domain-valid payload (about 2.4 MB, see TransportLimits) and below Kestrel's
        // 30 MB default, or it either rejects valid input or bounds nothing worth bounding.
        Assert.InRange(TransportLimits.MaxRequestBodyBytes, 3L * 1024 * 1024, 8L * 1024 * 1024);
    }

    [Fact]
    public void BothJsonConfigurations_CarryTheSameFiniteDepthLimit()
    {
        var mvcJson = _factory.Services
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value;
        var httpJson = _factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value;

        Assert.Equal(TransportLimits.MaxJsonDepth, mvcJson.JsonSerializerOptions.MaxDepth);
        Assert.Equal(TransportLimits.MaxJsonDepth, httpJson.SerializerOptions.MaxDepth);
    }
}
