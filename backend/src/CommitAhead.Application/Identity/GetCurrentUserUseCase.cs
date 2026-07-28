namespace CommitAhead.Application.Identity;

public sealed class GetCurrentUserUseCase
{
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserUseCase(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public CurrentUserResult Execute()
    {
        return new CurrentUserResult(_currentUser.UserId, _currentUser.Email);
    }
}

public sealed record CurrentUserResult(Guid UserId, string Email);
