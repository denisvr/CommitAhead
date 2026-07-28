---
status: accepted
date: 2026-07-28
---

# Feature-folder use cases without MediatR

## Context

Clean Architecture .NET projects conventionally use MediatR for command/query dispatch: controllers call `mediator.Send(command)`, and handlers implement `IRequestHandler<TRequest, TResponse>`. This introduces an abstraction layer between controllers and application logic whose value is contested when there is no runtime dispatch requirement.

## Decision

MediatR is excluded from the project. Each operation is a concrete use case class (e.g. `CreateStudyItemUseCase`, `ApplyAnalysisDraftUseCase`) with a single `ExecuteAsync` method, injected directly into a thin controller. Generic `IUseCase<TRequest, TResponse>` interfaces are also excluded.

Use case classes are organised under feature folders:
```
Features/
  StudyItems/
    CreateStudyItem/
      CreateStudyItemController.cs
      CreateStudyItemUseCase.cs
    SubmitStudyReview/
      ...
  JobAnalyses/
    AnalyzeJobAnalysis/
      ...
```

Cross-cutting concerns (auth validation, CSRF, error mapping, structured logging) are handled by ASP.NET middleware, filters, and decorators.

## Consequences

- Call sites are explicit and IDE-navigable: F12 on a use case call resolves directly to the implementation.
- Controllers are thin: they bind the request, call the use case, and map the result to an HTTP response.
- Pipeline behaviours (MediatR's mechanism for cross-cutting concerns) are replaced by middleware and filters, which are the native ASP.NET Core mechanism for the same purpose.
- The folder structure mirrors the domain's aggregate boundaries, making it easy to locate all operations on a given aggregate.

## Considered Alternatives

MediatR was the primary alternative. Its dispatch abstraction adds value when handlers must be resolved at runtime (e.g. plugin architectures), when pipeline behaviours are extensively composed, or when commands fan out to multiple handlers. None of these apply here. The abstraction would make call sites harder to trace without providing a compensating benefit.
