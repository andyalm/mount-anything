using Microsoft.Build.Framework;

namespace MountAnything.Hosting.Build;

internal static class MsBuildExtensions
{
    public static TTask ExecTask<TTask>(this ITask parentTask, Func<TTask> createTask) where TTask : ITask
    {
        var childTask = createTask();
        childTask.BuildEngine = parentTask.BuildEngine;
        childTask.HostObject = parentTask.HostObject;
        if(!childTask.Execute())
        {
            throw new ChildTaskFailedException("The child task '" + childTask.GetType().Name + "' failed when being called from the parent task '" + parentTask.GetType().Name + "'.");
        }

        return childTask;
    }

    public static string? FullPath(this ITaskItem? item)
    {
        return item?.GetMetadata("FullPath");
    }
}

public class ChildTaskFailedException : ApplicationException
{
    public ChildTaskFailedException(string message) : base(message) {}
}
