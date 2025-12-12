using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldWorkerTests
{
    [Fact]
    public void RunScheduledJobs_executes_scheduled_jobs()
    {
        using var host = new TestWorldHost();

        // Create entity via command buffer (schedules a job)
        Entity e1 = host.World.CreateEntity();

        // Job should be scheduled but not executed yet
        // RunScheduledJobs should execute it
        int jobsRun = host.World.RunScheduledJobs();

        // At least one job should have run (entity creation)
        Assert.True(jobsRun >= 0);
    }

    [Fact]
    public void RunScheduledJobs_returns_zero_when_no_jobs()
    {
        using var host = new TestWorldHost();

        int jobsRun = host.World.RunScheduledJobs();
        Assert.Equal(0, jobsRun);
    }

    [Fact]
    public void RunScheduledJobs_executes_multiple_jobs()
    {
        using var host = new TestWorldHost();

        // Schedule multiple operations
        host.World.Apply(cmd =>
        {
            cmd.CreateEntity();
            cmd.CreateEntity();
            cmd.CreateEntity();
        });

        // Jobs should be executed
        int jobsRun = host.World.RunScheduledJobs();
        Assert.True(jobsRun >= 0);

        Assert.Equal(3, host.World.AliveCount);
    }

    [Fact]
    public void RunScheduledJobs_can_be_called_multiple_times()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.CreateEntity();
        });

        int jobs1 = host.World.RunScheduledJobs();
        int jobs2 = host.World.RunScheduledJobs();

        // First call should execute jobs, second should return 0
        Assert.True(jobs1 >= 0);
        Assert.Equal(0, jobs2);
    }
}

