namespace MyOptiAlloySite.Business.Commerce.Seeding;

/// <summary>
/// One unit of commerce seeding. Steps run in <see cref="Order"/> and each must be
/// safe to run repeatedly.
/// </summary>
public interface ISeedStep
{
    /// <summary>Position in the pipeline. Lower runs first.</summary>
    int Order { get; }

    /// <summary>Shown in the job's progress output.</summary>
    string Name { get; }

    SeedOutcome Execute(SeedRunContext context);

    /// <summary>
    /// Removes what <see cref="Execute"/> created, matched against the seed dataset so
    /// nothing outside it is touched. Runs in reverse pipeline order.
    /// </summary>
    SeedOutcome Remove(SeedRunContext context);
}
