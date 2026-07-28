using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace CommitAhead.Api.Security;

/// <summary>
/// Seals the session-start UTC timestamp so /auth/refresh can enforce the ADR-0006 7-day
/// absolute timeout by explicitly computing elapsed time, rather than trusting the cookie's own
/// (browser-only-enforced, unauthenticated) presence and expiry.
/// </summary>
public sealed class SessionStartToken
{
    private const string Purpose = "CommitAhead.SessionStart.v1";

    private readonly IDataProtector _protector;

    public SessionStartToken(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(DateTimeOffset startedAtUtc)
    {
        return _protector.Protect(startedAtUtc.ToUnixTimeSeconds().ToString());
    }

    public bool TryGetStartedAtUtc(string protectedValue, out DateTimeOffset startedAtUtc)
    {
        try
        {
            var unprotected = _protector.Unprotect(protectedValue);
            if (long.TryParse(unprotected, out var unixSeconds))
            {
                startedAtUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                return true;
            }
        }
        catch (CryptographicException)
        {
            // Tampered, forged, or sealed under a rotated/unknown key — treat as no session.
        }

        startedAtUtc = default;
        return false;
    }
}
