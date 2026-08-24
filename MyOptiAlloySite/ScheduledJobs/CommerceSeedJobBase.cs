using System.Globalization;
using System.Text;
using EPiServer.Scheduler;
using MyOptiAlloySite.Business.Commerce.Seeding;

namespace MyOptiAlloySite.ScheduledJobs;

/// <summary>
///     Shared plumbing for the seed and teardown jobs: the environment guard, the ordered
///     walk over the pipeline, progress reporting and stop handling.
/// </summary>
public abstract class CommerceSeedJobBase : ScheduledJobBase
{
    /// <summary>
    ///     Seeding writes directly to the catalog, so both jobs are refused outside
    ///     Development unless this is explicitly turned on.
    /// </summary>
    public const string AllowOutsideDevelopmentKey = "Commerce:Seeding:AllowOutsideDevelopment";

    private readonly IEnumerable<ISeedStep> _steps;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    private bool _stopRequested;

    protected CommerceSeedJobBase(
        IEnumerable<ISeedStep> steps,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _steps = steps;
        _environment = environment;
        _configuration = configuration;
        IsStoppable = true;
    }

    /// <summary>Runs steps in reverse pipeline order when true.</summary>
    protected abstract bool IsReverse { get; }

    protected abstract string CompletedVerb { get; }

    protected abstract SeedOutcome RunStep(ISeedStep step, SeedRunContext context);

    public override void Stop()
    {
        _stopRequested = true;
    }

    public override string Execute()
    {
        _stopRequested = false;

        if (!_environment.IsDevelopment() && !_configuration.GetValue<bool>(AllowOutsideDevelopmentKey))
            return $"Refused: this job writes sample data into the catalog and this is the " +
                   $"'{_environment.EnvironmentName}' environment. Set '{AllowOutsideDevelopmentKey}' " +
                   "to true if that is genuinely intended.";

        var log = new StringBuilder();
        var total = SeedOutcome.Empty;
        var context = new SeedRunContext(
            message =>
            {
                log.AppendLine(message);
                OnStatusChanged(message);
            },
            () => _stopRequested);

        var ordered = IsReverse
            ? _steps.OrderByDescending(s => s.Order)
            : _steps.OrderBy(s => s.Order);

        foreach (var step in ordered)
        {
            OnStatusChanged($"Running: {step.Name}");

            try
            {
                var outcome = RunStep(step, context);
                total = total.Add(outcome);
                log.AppendLine(CultureInfo.InvariantCulture, $"[{step.Name}] {outcome}");
            }
            catch (SeedStoppedException)
            {
                log.AppendLine(CultureInfo.InvariantCulture, $"[{step.Name}] stopped by operator.");
                return $"Stopped. {total}{Environment.NewLine}{log}";
            }
        }

        return $"{CompletedVerb} {total}{Environment.NewLine}{log}";
    }
}