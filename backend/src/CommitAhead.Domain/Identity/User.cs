namespace CommitAhead.Domain.Identity;

public sealed class User
{
    public Guid Id { get; }
    public string SupabaseUserId { get; }
    public string Email { get; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAtUtc { get; }

    public User(Guid id, string supabaseUserId, string email, DateTime createdAtUtc, bool isEnabled = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(supabaseUserId))
        {
            throw new ArgumentException("SupabaseUserId is required.", nameof(supabaseUserId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Id = id;
        SupabaseUserId = supabaseUserId;
        Email = email;
        CreatedAtUtc = createdAtUtc;
        IsEnabled = isEnabled;
    }

    public void Disable()
    {
        IsEnabled = false;
    }
}
