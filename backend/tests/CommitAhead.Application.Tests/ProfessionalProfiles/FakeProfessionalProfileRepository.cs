using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.Tests.ProfessionalProfiles;

/// <summary>Handwritten in-memory fake, per docs/testing/strategy.md Layer 2.</summary>
public sealed class FakeProfessionalProfileRepository : IProfessionalProfileRepository
{
    private readonly List<ProfessionalProfile> _profiles = [];

    public IReadOnlyList<ProfessionalProfile> Profiles => _profiles;

    public Task<ProfessionalProfile?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var profile = _profiles.SingleOrDefault(p => p.OwnerUserId == ownerUserId);
        return Task.FromResult(profile);
    }

    public Task AddAsync(ProfessionalProfile profile, CancellationToken cancellationToken)
    {
        _profiles.Add(profile);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }
}
