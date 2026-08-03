namespace CommitAhead.Api.Security;

/// <summary>
/// Marks a controller (or action) whose endpoints read/write owner-scoped Phase 1 business tables
/// (StudyItem, StudyReview, ScoringConfigOverride, EvidenceLink) — RlsContextMiddleware only opens
/// an RLS-scoped transaction for these, not for every authenticated request. Endpoints that never
/// touch those tables (e.g. GET /api/me) get no needless transaction, and lightweight test hosts
/// with no real database (e.g. AuthTestWebApplicationFactory) are unaffected.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
internal sealed class UsesOwnerScopedDataAttribute : Attribute;
