namespace Vonage_SSW_Workshop.Domain.Common.Base;

/// <summary>
/// Cluster of objects treated as a single unit.
/// Can contain entities, value objects, and other aggregates.
/// Enforce business rules (i.e. invariants)
/// Can be created externally.
/// Represent a transactional boundary (i.e. all changes are saved or none are saved)
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
{
}