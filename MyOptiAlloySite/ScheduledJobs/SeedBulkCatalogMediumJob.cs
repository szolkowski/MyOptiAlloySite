using EPiServer.Scheduler;
using MyOptiAlloySite.Business.Commerce.Seeding.Bulk;

namespace MyOptiAlloySite.ScheduledJobs;

/// <summary>
/// Creates the 10,000-entry synthetic dataset under its own category. Does not overlap
/// the other bulk datasets: separate category, separate entry-code prefix.
/// </summary>
[ScheduledJob(
    GUID = "C4D8E1F5-7A93-4B20-8E6C-1F0A5D3B9E72",
    DisplayName = "Seed bulk catalog (10k)",
    Description = "Creates 10,000 catalog entries under the 'Bulk 10k' category. Resumable and idempotent.",
    DefaultEnabled = false)]
public sealed class SeedBulkCatalogMediumJob(
    BulkCatalogSeeder seeder,
    IWebHostEnvironment environment,
    IConfiguration configuration) : BulkCatalogJobBase(seeder, environment, configuration)
{
    protected override BulkCatalogProfile Profile => BulkCatalogProfile.Medium;
}
