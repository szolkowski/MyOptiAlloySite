namespace MyOptiAlloySite.Business.Commerce.Seeding;

/// <summary>
/// Lets a seed step report progress and notice that the running job was stopped,
/// without knowing anything about the scheduled job hosting it.
/// </summary>
public sealed class SeedRunContext(Action<string> report, Func<bool> isStopRequested)
{
    public bool IsStopRequested => isStopRequested();

    public void Report(string message) => report(message);

    /// <summary>
    /// Steps call this between units of work so a stop request takes effect promptly
    /// rather than after the whole step finishes.
    /// </summary>
    public void ThrowIfStopRequested()
    {
        if (IsStopRequested)
        {
            throw new SeedStoppedException();
        }
    }
}

/// <summary>
/// Thrown when an operator stops the job mid-run. Not an error.
/// </summary>
public sealed class SeedStoppedException() : Exception("Seeding was stopped.");
