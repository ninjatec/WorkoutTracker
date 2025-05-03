using System;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Custom Hangfire JobActivator that resolves instances from the DI container
    /// </summary>
    public class HangfireActivator : JobActivator
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public HangfireActivator(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public override object ActivateJob(Type jobType)
        {
            // Create a new scope for each job activation
            var scope = _serviceScopeFactory.CreateScope();
            
            // Try to resolve the job type from the scoped service provider
            return scope.ServiceProvider.GetService(jobType) ?? base.ActivateJob(jobType);
        }
        
        // Enable proper disposal of job scopes
        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            return new HangfireActivatorScope(_serviceScopeFactory.CreateScope());
        }
    }
    
    // Add a scope class to handle disposing of the service scope
    public class HangfireActivatorScope : JobActivatorScope
    {
        private readonly IServiceScope _serviceScope;
        
        public HangfireActivatorScope(IServiceScope serviceScope)
        {
            _serviceScope = serviceScope;
        }
        
        public override object Resolve(Type type)
        {
            return _serviceScope.ServiceProvider.GetService(type);
        }
        
        public override void DisposeScope()
        {
            _serviceScope.Dispose();
        }
    }
}