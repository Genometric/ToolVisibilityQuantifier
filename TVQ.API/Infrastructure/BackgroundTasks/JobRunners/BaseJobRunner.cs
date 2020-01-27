﻿using Genometric.TVQ.API.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Genometric.TVQ.API.Infrastructure.BackgroundTasks
{
    public abstract class BaseJobRunner<T> : BackgroundService
        where T : BaseJob
    {
        protected DbSet<T> DbSet { get; }
        protected TVQContext Context { get; }
        protected IServiceProvider Services { get; }
        protected ILogger<BaseJobRunner<T>> Logger { get; }
        protected IBaseBackgroundTaskQueue<T> Queue { get; }

        protected BaseJobRunner(
            TVQContext context,
            IServiceProvider services,
            ILogger<BaseJobRunner<T>> logger,
            IBaseBackgroundTaskQueue<T> queue)
        {
            Context = context;
            Services = services;
            Logger = logger;
            Queue = queue;
            DbSet = context.Set<T>();
        }

        protected abstract T AugmentJob(T job);

        protected abstract Task RunJobAsync(IServiceScope scope, T job, CancellationToken cancellationToken);

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"{typeof(T)} job runner is starting.");

            foreach (var job in DbSet.Where(x => x.Status == State.Queued || x.Status == State.Running))
            {
                Queue.Enqueue(job);
                Logger.LogInformation($"The unfinished job {job.ID} of type {nameof(T)} is re-queued.");
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                IServiceScope scope = null;
                var dequeuedJob = await Queue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                var job = AugmentJob(dequeuedJob);
                Context.Attach(job);

                try
                {
                    scope = Services.CreateScope();
                    job.Status = State.Running;
                    await Context.SaveChangesAsync().ConfigureAwait(false);
                    await RunJobAsync(scope, job, cancellationToken).ConfigureAwait(false);
                    job.Status = State.Completed;
                }
                catch (Exception e)
                {
                    var msg = $"Error occurred executing job {job.ID} of type {nameof(job)}: {e.Message}";
                    job.Status = State.Failed;
                    job.Message = msg;
                    Logger.LogError(e, msg);
                }
                finally
                {
                    await Context.SaveChangesAsync().ConfigureAwait(false);
                    if (scope != null)
                        scope.Dispose();
                }
            }

            Logger.LogInformation($"{nameof(T)} job runner is stopping.");
        }
    }
}
