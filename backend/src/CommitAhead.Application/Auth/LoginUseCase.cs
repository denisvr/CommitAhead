using CommitAhead.Application.Identity;
using CommitAhead.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Application.Auth;

/// <summary>
/// Always returns a PKCE code verifier so the response shape is identical whether or not the
/// email is provisioned (ADR-0015: closed login — this prevents enumeration through the response
/// status/body; the timing difference between calling Supabase or not is a separate, accepted
/// residual risk mitigated only by rate limiting, see docs/security/threat-model.md). Supabase is
/// only called — the only externally-visible/timed side effect — when the email matches an
/// existing, enabled User; the controller returns the same generic response regardless of
/// outcome. A Supabase failure (network error, non-success status, provider timeout) is swallowed
/// here for the same reason: a provisioned user's email must not produce a different HTTP status
/// than an unknown one just because the external call happened to fail. Only a safe event/error
/// type is logged — never the email, never the raw exception (message/stack trace).
/// </summary>
public sealed class LoginUseCase
{
    private readonly ISupabaseAuthClient _authClient;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<LoginUseCase> _logger;

    public LoginUseCase(ISupabaseAuthClient authClient, IUserRepository userRepository, ILogger<LoginUseCase> logger)
    {
        _authClient = authClient;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.Normalize(email);
        var codeVerifier = PkceChallenge.GenerateCodeVerifier();
        var codeChallenge = PkceChallenge.ComputeCodeChallenge(codeVerifier);

        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user is not null && user.IsEnabled)
        {
            try
            {
                await _authClient.InitiateMagicLinkAsync(normalizedEmail, codeChallenge, cancellationToken);
            }
            // HttpClient provider timeouts also throw via the OperationCanceledException
            // hierarchy (TaskCanceledException) — those must be treated as an ordinary provider
            // failure, not propagated. Only genuine cancellation of OUR OWN cancellationToken
            // (the caller's request being aborted) should propagate.
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // Never let a Supabase failure surface as a different response than an unknown
                // email would get — never logs the email, never the exception message/stack trace.
                _logger.LogError("Failed to initiate a Supabase magic link. Exception type: {ExceptionType}", ex.GetType().Name);
            }
        }

        return codeVerifier;
    }
}
