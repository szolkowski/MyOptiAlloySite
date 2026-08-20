using EPiServer.Scheduler;
using MyOptiAlloySite.Business.Commerce.Seeding.Bulk;

namespace MyOptiAlloySite.ScheduledJobs;

/// <summary>
/// Creates the 100,000-entry synthetic dataset under its own category. Does not overlap
/// the other bulk datasets: separate category, separate entry-code prefix.
/// </summary>
[ScheduledJob(
    GUID = "E7F1A2B8-3C64-4D95-A0E8-9B2C6F4D1A53",
    DisplayName = "Seed bulk catalog (100k)",
    Description = "Creates 100,000 catalog entries under the 'Bulk 100k' category. Resumable and idempotent.",
    DefaultEnabled = false)]
public sealed class SeedBulkCatalogLargeJob(
    BulkCatalogSeeder seeder,
    IWebHostEnvironment environment,
    IConfiguration configuration) : BulkCatalogJobBase(seeder, environment, configuration)
{
    protected override BulkCatalogProfile Profile => BulkCatalogProfile.Large;
}
