using CommitAhead.Api.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace CommitAhead.Api.Tests.Security;

/// <summary>
/// Proves "DataProtection:KeyRingPath" (ADR-0021) actually persists the key ring to disk, not just
/// that the option compiles — a fresh IServiceProvider standing in for a container restart can only
/// unprotect a payload sealed by an earlier one if the keys genuinely made it to that path.
/// </summary>
public class DataProtectionKeyPersistenceTests
{
    private sealed class StubWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Docker";
        public string ApplicationName { get; set; } = "CommitAhead.Api.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IDataProtectionProvider BuildDataProtectionProvider(string keyRingPath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataProtection:KeyRingPath"] = keyRingPath })
            .Build();

        var services = new ServiceCollection();
        services.AddCommitAheadSecurity(new StubWebHostEnvironment(), configuration);
        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }

    [Fact]
    public void ProtectedPayload_CanBeUnprotectedByASeparateServiceProviderPointedAtTheSameKeyRingPath()
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), "commitahead-dp-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(keyRingPath);

        try
        {
            var firstProtector = BuildDataProtectionProvider(keyRingPath).CreateProtector("CommitAhead.Tests");
            var protectedPayload = firstProtector.Protect("a secret session value");

            // A brand-new provider, built from a brand-new key ring path lookup, is the closest
            // in-process stand-in for the container restarting — it only succeeds if the key ring
            // was truly written to keyRingPath, not merely cached in the first provider's memory.
            var secondProtector = BuildDataProtectionProvider(keyRingPath).CreateProtector("CommitAhead.Tests");
            var unprotected = secondProtector.Unprotect(protectedPayload);

            Assert.Equal("a secret session value", unprotected);
        }
        finally
        {
            Directory.Delete(keyRingPath, recursive: true);
        }
    }

    [Fact]
    public void WithoutAKeyRingPathConfigured_StillResolvesAWorkingDataProtectionProvider()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddCommitAheadSecurity(new StubWebHostEnvironment(), configuration);

        var provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        var protector = provider.CreateProtector("CommitAhead.Tests");

        Assert.Equal("round trips", protector.Unprotect(protector.Protect("round trips")));
    }
}
