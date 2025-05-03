using System;
using Hangfire;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Custom Hangfire JobActivator that resolves instances from the DI container
    /// </summary>
    public class HangfireActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override object ActivateJob(Type jobType)
        {
            return _serviceProvider.GetService(jobType) ?? base.ActivateJob(jobType);
        }
    }
}