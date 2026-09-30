using EPiServer.Scheduler;
using MyOptiAlloySite.Business.Commerce.Seeding.Bulk;
using OptiPowerTools.ScheduledJobsInsights.Logging;

namespace MyOptiAlloySite.ScheduledJobs;

/// <summary>
/// Creates the 1,000-entry synthetic dataset under its own category. Does not overlap
/// the other bulk datasets: separate category, separate entry-code prefix.
/// </summary>
[ScheduledJob(
    GUID = "A1B7C3D9-4E52-4F86-9C10-3D5E7A0B2C41",
    DisplayName = "Seed bulk catalog (1k)",
    Description = "Creates 1,000 catalog entries under the 'Bulk 1k' category. Resumable and idempotent.",
    DefaultEnabled = false)]
public sealed class SeedBulkCatalogSmallJob(
    BulkCatalogSeeder seeder,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    JobLoggingContext context) : BulkCatalogJobBase(seeder, environment, configuration, context)
{
    protected override BulkCatalogProfile Profile => BulkCatalogProfile.Small;
}
