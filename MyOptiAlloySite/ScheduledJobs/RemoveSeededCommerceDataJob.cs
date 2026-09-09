using EPiServer.Scheduler;
using MyOptiAlloySite.Business.Commerce.Seeding;
using OptiPowerTools.ScheduledJobsInsights.Logging;

namespace MyOptiAlloySite.ScheduledJobs;

/// <summary>
/// Removes what <see cref="SeedCommerceDataJob"/> created, matched against the seed
/// dataset. Runs the pipeline in reverse so dependants go before their dependencies.
/// </summary>
[ScheduledJob(
    GUID = "8F0E5B27-1A3D-4C6E-B0A9-7D2F4C8E1B55",
    DisplayName = "Remove seeded Commerce data",
    Description = "Deletes the seeded catalog, prices, inventory, associations, contacts and warehouse. Only touches items named in SeedData/commerce-catalog.json.",
    DefaultEnabled = false)]
public sealed class RemoveSeededCommerceDataJob(
    IEnumerable<ISeedStep> steps,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    JobLoggingContext context) : CommerceSeedJobBase(steps, environment, configuration, context)
{
    protected override bool IsReverse => true;

    protected override string CompletedVerb => "Teardown complete.";

    protected override SeedOutcome RunStep(ISeedStep step, SeedRunContext context) => step.Remove(context);
}
