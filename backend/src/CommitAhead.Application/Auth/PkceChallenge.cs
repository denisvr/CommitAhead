using System.Security.Cryptography;
using System.Text;

namespace CommitAhead.Application.Auth;

/// <summary>
/// PKCE (RFC 7636) code_verifier/code_challenge generation for the magic-link flow (ADR-0006).
/// The backend is the PKCE client here — there is no browser-side PKCE step.
/// </summary>
public static class PkceChallenge
{
    public static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    public static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
