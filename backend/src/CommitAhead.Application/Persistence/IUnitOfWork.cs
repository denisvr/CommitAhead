namespace CommitAhead.Application.Persistence;

/// <summary>
/// One explicit transaction boundary spanning several repositories' own writes — used where a
/// single aggregate's own <c>SaveChangesAsync</c> isn't enough (AnalyzeJobAnalysisUseCase's
/// reservation-reconciliation step and its draft/completion step both need this; a future
/// ApplyAnalysisDraftUseCase is expected to reuse it too). Deliberately just this one method — not
/// a generic repository/session abstraction or transaction framework.
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
