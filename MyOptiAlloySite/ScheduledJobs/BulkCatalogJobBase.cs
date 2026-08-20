using System.Diagnostics;
using System.Text;
using EPiServer.Scheduler;
using MyOptiAlloySite.Business.Commerce.Seeding;
using MyOptiAlloySite.Business.Commerce.Seeding.Bulk;

namespace MyOptiAlloySite.ScheduledJobs;

/// <summary>
/// Shared plumbing for the bulk catalog jobs: the environment guard, stop handling and
/// timing. Each dataset gets its own job so the three can be run, stopped and re-run
/// independently.
/// </summary>
public abstract class BulkCatalogJobBase(
    BulkCatalogSeeder seeder,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ScheduledJobBase
{
    private bool _stopRequested;

    protected abstract BulkCatalogProfile Profile { get; }

    public override void Stop() => _stopRequested = true;

    public override string Execute()
    {
        _stopRequested = false;
        IsStoppable = true;

        if (!environment.IsDevelopment()
            && !configuration.GetValue<bool>(CommerceSeedJobBase.AllowOutsideDevelopmentKey))
        {
            return $"Refused: this job writes {Profile.TotalEntries:N0} synthetic entries into the " +
                   $"catalog and this is the '{environment.EnvironmentName}' environment. Set " +
                   $"'{CommerceSeedJobBase.AllowOutsideDevelopmentKey}' to true if that is genuinely intended.";
        }

        var log = new StringBuilder();
        var context = new SeedRunContext(
            message =>
            {
                log.AppendLine(message);
                OnStatusChanged(message);
            },
            () => _stopRequested);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var outcome = seeder.Seed(Profile, context);
            stopwatch.Stop();

            return $"{Profile.CategoryName}: {outcome} in {Format(stopwatch.Elapsed)}." +
                   $"{Environment.NewLine}{log}";
        }
        catch (SeedStoppedException)
        {
            stopwatch.Stop();

            // Progress is kept: the job resumes from where it stopped on the next run.
            return $"{Profile.CategoryName}: stopped after {Format(stopwatch.Elapsed)}. " +
                   $"Run again to resume from where it left off.{Environment.NewLine}{log}";
        }
    }

    private static string Format(TimeSpan elapsed) =>
        elapsed.TotalMinutes >= 1
            ? $"{elapsed.TotalMinutes:N1} min"
            : $"{elapsed.TotalSeconds:N1} s";
}
