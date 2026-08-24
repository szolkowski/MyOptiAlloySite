namespace MyOptiAlloySite.Business.Commerce.Seeding;

/// <summary>
/// What a seed step did. Seeding is idempotent, so a re-run should report
/// everything as updated or unchanged and nothing as created.
/// </summary>
public readonly record struct SeedOutcome(int Created, int Updated, int Unchanged, int Removed = 0)
{
    public static SeedOutcome Empty => new(0, 0, 0, 0);

    public int Total => Created + Updated + Unchanged + Removed;

    public SeedOutcome WithCreated() => this with { Created = Created + 1 };

    public SeedOutcome WithUpdated() => this with { Updated = Updated + 1 };

    public SeedOutcome WithUnchanged() => this with { Unchanged = Unchanged + 1 };

    public SeedOutcome WithRemoved(int count = 1) => this with { Removed = Removed + count };

    public SeedOutcome Add(SeedOutcome other) => new(
        Created + other.Created,
        Updated + other.Updated,
        Unchanged + other.Unchanged,
        Removed + other.Removed);

    public override string ToString() => Removed > 0
        ? $"{Removed} removed"
        : $"{Created} created, {Updated} updated, {Unchanged} unchanged";
}
