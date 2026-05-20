namespace CopyCat.Web.Services;

internal sealed class ForwardingJobUpdateNotifier
{
    public event Func<Task>? JobsChanged;

    public async Task NotifyJobsChangedAsync()
    {
        Func<Task>? handlers = JobsChanged;
        if (handlers is null)
        {
            return;
        }

        Delegate[] subscribers = handlers.GetInvocationList();
        foreach (Func<Task> subscriber in subscribers.Cast<Func<Task>>())
        {
            await subscriber();
        }
    }
}
