namespace Vonage_SSW_Workshop.Domain.Common.Base;

/// <summary>
/// Entities have an ID and a lifecycle.
/// They can be created within the domain, but not externally.
/// Enforce business rules (i.e. invariants)
/// </summary>
public abstract class Entity<TId>
{
    public TId Id { get; set; } = default!;
}