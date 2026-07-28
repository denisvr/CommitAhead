namespace CommitAhead.Api.Security;

/// <summary>
/// Exempts an action from CSRF validation. Only /auth/login should ever carry this — it runs
/// before any session exists, so there is no CSRF cookie yet to validate against.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
internal sealed class SkipCsrfAttribute : Attribute
{
}
