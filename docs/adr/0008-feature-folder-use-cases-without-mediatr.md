# Feature-folder use cases without MediatR

MediatR is the conventional dispatch mechanism in Clean Architecture .NET projects: commands implement `IRequest<T>`, handlers implement `IRequestHandler<TRequest, TResponse>`, and controllers send to the mediator. We exclude MediatR entirely.

Each operation is a concrete use case class (e.g., `CreateStudyItemUseCase`, `ApplyAnalysisDraftUseCase`) with a single `ExecuteAsync` method, injected directly into a thin controller. Use case classes are organised under feature folders (`Features/StudyItems/`, `Features/JobAnalyses/`, etc.) mirroring the domain structure. Cross-cutting concerns (auth, logging, validation, CSRF) are handled by ASP.NET middleware, filters, and decorators rather than pipeline behaviours.

The reason: MediatR hides call sites behind an abstraction that provides no benefit when there is no runtime dispatch requirement. Concrete injection makes dependencies explicit, keeps use cases IDE-navigable by F12, and removes a framework that encourages handler proliferation. Generic `IUseCase<TRequest, TResponse>` interfaces are also excluded for the same reason.
