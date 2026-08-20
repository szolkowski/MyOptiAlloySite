using EPiServer.Scheduler;
using MyOptiAlloySite.Business.Commerce.Seeding;

namespace MyOptiAlloySite.ScheduledJobs;

/// <summary>
/// Runs every commerce seed step in order. Safe to run repeatedly: a second run reports
/// everything as unchanged.
/// </summary>
[ScheduledJob(
    GUID = "2D6C4A55-9C64-4A8E-9E2B-6B4C1E7A0F31",
    DisplayName = "Seed Commerce data",
    Description = "Populates market, warehouse, catalog, products, prices, inventory, associations and contacts from SeedData/commerce-catalog.json. Idempotent.",
    DefaultEnabled = false)]
public sealed class SeedCommerceDataJob(
    IEnumerable<ISeedStep> steps,
    IWebHostEnvironment environment,
    IConfiguration configuration) : CommerceSeedJobBase(steps, environment, configuration)
{
    protected override bool IsReverse => false;

    protected override string CompletedVerb => "Seeding complete.";

    protected override SeedOutcome RunStep(ISeedStep step, SeedRunContext context) => step.Execute(context);
}
