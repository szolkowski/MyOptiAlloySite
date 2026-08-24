using System.Text;
using EPiServer.Scheduler;
using MyOptiAlloySite.Business.Commerce.Seeding;
using MyOptiAlloySite.Business.Commerce.Seeding.Bulk;

namespace MyOptiAlloySite.ScheduledJobs;

/// <summary>
/// Removes every bulk catalog dataset. Deleting the category takes its products, variants,
/// prices and inventory with it, so this is one delete per dataset rather than a walk over
/// a hundred thousand entries.
/// </summary>
[ScheduledJob(
    GUID = "B3C9D5E7-2F48-4A61-B8D0-6E1C3A9F7B24",
    DisplayName = "Remove bulk catalog data",
    Description = "Deletes the 'Bulk 1k', 'Bulk 10k' and 'Bulk 100k' categories and everything under them. Leaves the hand-authored seed data alone.",
    DefaultEnabled = false)]
public sealed class RemoveBulkCatalogDataJob(
    BulkCatalogSeeder seeder,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ScheduledJobBase
{
    private bool _stopRequested;

    public override void Stop() => _stopRequested = true;

    public override string Execute()
    {
        _stopRequested = false;
        IsStoppable = true;

        if (!environment.IsDevelopment()
            && !configuration.GetValue<bool>(CommerceSeedJobBase.AllowOutsideDevelopmentKey))
        {
            return $"Refused: this is the '{environment.EnvironmentName}' environment. Set " +
                   $"'{CommerceSeedJobBase.AllowOutsideDevelopmentKey}' to true if that is genuinely intended.";
        }

        var log = new StringBuilder();
        var total = SeedOutcome.Empty;
        var context = new SeedRunContext(
            message =>
            {
                log.AppendLine(message);
                OnStatusChanged(message);
            },
            () => _stopRequested);

        foreach (var profile in BulkCatalogProfile.All)
        {
            try
            {
                total = total.Add(seeder.Remove(profile, context));
            }
            catch (SeedStoppedException)
            {
                return $"Stopped. {total}{Environment.NewLine}{log}";
            }
        }

        return $"Bulk catalog teardown complete. {total}{Environment.NewLine}{log}";
    }
}
