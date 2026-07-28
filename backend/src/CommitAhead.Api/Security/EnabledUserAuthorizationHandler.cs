using CommitAhead.Api.Identity;
using CommitAhead.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace CommitAhead.Api.Security;

internal sealed class EnabledUserAuthorizationHandler : AuthorizationHandler<EnabledUserRequirement>
{
    private readonly IUserRepository _userRepository;
    private readonly CurrentUserAccessor _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EnabledUserAuthorizationHandler(IUserRepository userRepository, CurrentUserAccessor currentUser, IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EnabledUserRequirement requirement)
    {
        var supabaseUserId = context.User.FindFirst("sub")?.Value;
        if (supabaseUserId is null)
        {
            context.Fail();
            return;
        }

        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var user = await _userRepository.GetBySupabaseUserIdAsync(supabaseUserId, cancellationToken);

        if (user is null || !user.IsEnabled)
        {
            context.Fail();
            return;
        }

        _currentUser.Set(user.Id, user.Email);
        context.Succeed(requirement);
    }
}
