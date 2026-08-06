using System.Reflection;
using CommitAhead.Application.CVPresentations;
using CommitAhead.Application.Identity;
using CommitAhead.Application.Persistence;
using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Application.StudyItems;
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
    public void Controllers_ShouldNotInjectRepositoriesDirectly()
    {
        // Repository interfaces live in CommitAhead.Application, so a controller injecting one
        // directly would not trip Controllers_ShouldOnlyDependOnApplication_NotInfrastructureOrDomain
        // above — controllers must depend on use cases, not repositories, even though both live in
        // the same allowed assembly.
        var controllerTypes = Types.InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .GetTypes();

        var violations = new List<string>();

        foreach (var controllerType in controllerTypes)
        {
            var constructor = controllerType.GetConstructors().SingleOrDefault();
            if (constructor is null)
            {
                continue;
            }

            violations.AddRange(constructor.GetParameters()
                .Where(p => p.ParameterType.Name.EndsWith("Repository", StringComparison.Ordinal))
                .Select(p => $"{controllerType.Name}({p.ParameterType.Name})"));
        }

        Assert.Empty(violations);
    }

    // Explicit, non-vacuous list of every production persistence/query port declared in
    // Application — a suffix-only rule (e.g. "type name ends with Repository") would pass even
    // if a new port were added and never checked here, so each one is named individually and
    // each is asserted to have a real Infrastructure implementation, not just "none elsewhere."
    private static readonly Type[] PersistencePorts =
    [
        typeof(IUserRepository),
        typeof(IStudyItemRepository),
        typeof(IScoringConfigRepository),
        typeof(IRankedStudyQueueQuery),
        typeof(IEvidenceLinkQuery),
        typeof(IRlsSessionContext),
        typeof(IProfessionalProfileRepository),
        typeof(ICVPresentationRepository),
    ];

    [Fact]
    public void RepositoryImplementations_ShouldOnlyExistInInfrastructure()
    {
        var nonInfrastructureAssemblies = new[] { DomainAssembly, ApplicationAssembly, ApiAssembly };

        foreach (var port in PersistencePorts)
        {
            foreach (var assembly in nonInfrastructureAssemblies)
            {
                var matchingTypes = Types.InAssembly(assembly)
                    .That()
                    .ImplementInterface(port)
                    .GetTypes();

                Assert.Empty(matchingTypes);
            }

            var infrastructureImplementations = Types.InAssembly(InfrastructureAssembly)
                .That()
                .ImplementInterface(port)
                .GetTypes();

            Assert.NotEmpty(infrastructureImplementations);
        }
    }

    [Fact(Skip =
        "Pending: CLAUDE.md rule 5's IAIProvider half cannot be verified yet — Application declares no " +
        "IAIProvider interface until Phase 4. The repository half is now covered for real by " +
        "RepositoryImplementations_ShouldOnlyExistInInfrastructure above (IUserRepository/UserRepository). " +
        "Rewrite this test once Application declares IAIProvider: assert " +
        "Types.InAssembly(...).That().ImplementInterface(typeof(IAIProvider)) exist only in " +
        "CommitAhead.Infrastructure, excluding test fakes.")]
    public void AIProviderImplementations_ShouldOnlyExistInInfrastructure()
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
