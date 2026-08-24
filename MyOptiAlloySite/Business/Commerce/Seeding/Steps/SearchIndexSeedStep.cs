using EPiServer.Business.Commerce.ScheduledJobs;

namespace MyOptiAlloySite.Business.Commerce.Seeding.Steps;

/// <summary>
/// Rebuilds the catalog search index so freshly seeded entries are findable in the
/// Commerce UI. Delegates to Commerce's own indexing job rather than reimplementing it.
/// </summary>
/// <remarks>
/// Runs last: indexing entries before they exist would be wasted work.
/// </remarks>
public sealed class SearchIndexSeedStep(FullSearchIndexJob indexJob) : ISeedStep
{
    public int Order => 900;

    public string Name => "Catalog search index";

    public SeedOutcome Execute(SeedRunContext context)
    {
        context.ThrowIfStopRequested();

        var result = indexJob.Execute();
        context.Report($"Rebuilt catalog search index. {result}");

        return SeedOutcome.Empty.WithUpdated();
    }

    /// <summary>
    /// No-op: the index is derived data. Removing the entries is what empties it, and a
    /// later rebuild will reflect that.
    /// </summary>
    public SeedOutcome Remove(SeedRunContext context) => SeedOutcome.Empty;
}
