namespace CommitAhead.Domain;

/// <summary>
/// Thrown explicitly wherever domain or application code rejects a value a caller (ultimately, an
/// HTTP request body or query string) supplied — never for a genuine programming error. Api's
/// ValidationExceptionFilter maps only this specific type to 422, deliberately not a blanket
/// ArgumentException catch: a validation failure's message is always safe to return verbatim
/// because the throw site wrote it to be read by a client, but an unrelated ArgumentException from
/// framework or library code was never written with that in mind and could say anything.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
