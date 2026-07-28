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

    [Fact]
    public void RepositoryAndAIProviderImplementations_ShouldOnlyExistInInfrastructure()
    {
        var nonInfrastructureAssemblies = new[] { DomainAssembly, ApplicationAssembly, ApiAssembly };

        foreach (var assembly in nonInfrastructureAssemblies)
        {
            var matchingTypes = Types.InAssembly(assembly)
                .That()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Repository")
                .Or()
                .HaveNameEndingWith("AIProvider")
                .GetTypes();

            Assert.Empty(matchingTypes);
        }
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
