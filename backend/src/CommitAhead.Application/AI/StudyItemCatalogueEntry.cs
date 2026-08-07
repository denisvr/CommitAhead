using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.AI;

/// <summary>
/// The "compact StudyItem catalogue" every analyze command sends (docs/domain/use-cases.md) — just
/// enough for the AI to identify an existing StudyItem to link to (LinkProposal.TargetStudyItemId)
/// or to recognise a gap not yet represented in the queue. Never the item's full Details/Reviews —
/// those add real token cost for no benefit here.
/// </summary>
public sealed record StudyItemCatalogueEntry(Guid Id, string Title, StudyItemCategory Category, IReadOnlyList<string> Tags);
