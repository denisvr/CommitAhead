using System.Reflection;
using NetArchTest.Rules;

namespace CommitAhead.Api.Tests.Architecture;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = Assembly.Load("CommitAhead.Domain");
    private static readonly Assembly ApplicationAssembly = Assembly.Load("CommitAhead.Application");
    private static readonly Assembly InfrastructureAssembly = Assembly.Load("CommitAhead.Infrastructure");
    private static readonly Assembly ApiAssembly = Assembly.Load("CommitAhead.Api");

    [Fact]
    public void Domain_ShouldNotDependOn_ApplicationInfrastructureOrApi()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("CommitAhead.Application", "CommitAhead.Infrastructure", "CommitAhead.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureApiOrFrameworkPersistenceTypes()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "CommitAhead.Infrastructure",
                "CommitAhead.Api",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore",
                "Supabase")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("CommitAhead.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Controllers_ShouldOnlyDependOnApplication_NotInfrastructureOrDomain()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOnAny(
                "CommitAhead.Infrastructure",
                "CommitAhead.Domain",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact(Skip =
        "Pending: CLAUDE.md rule 5 (\"repository and IAIProvider production implementations exist only in " +
        "Infrastructure\") cannot be verified yet. Application defines no repository interfaces and no " +
        "IAIProvider yet (those land in Phase 1 and Phase 4), so there is nothing to check the rule against. " +
        "A name-suffix match (e.g. \"*Repository\", \"*AIProvider\") on a codebase with no such types would " +
        "pass vacuously and prove nothing — and adding placeholder interfaces solely to make this test pass " +
        "would be a speculative abstraction with no use case behind it. Rewrite this test once Application " +
        "declares IAIProvider and at least one repository interface: assert concrete implementations of those " +
        "interfaces (Types.InAssembly(...).That().ImplementInterface(typeof(IAIProvider)), etc.) exist only in " +
        "CommitAhead.Infrastructure, excluding test fakes.")]
    public void RepositoryAndAIProviderImplementations_ShouldOnlyExistInInfrastructure()
    {
    }

    private static string Describe(TestResult result)
    {
        if (result.IsSuccessful)
        {
            return string.Empty;
        }

        var failingTypeNames = result.FailingTypes?.Select(t => t.FullName) ?? [];
        return $"Failing types: {string.Join(", ", failingTypeNames)}";
    }
}
