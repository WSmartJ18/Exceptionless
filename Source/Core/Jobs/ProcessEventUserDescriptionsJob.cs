﻿using System;
using System.Linq;
using System.Threading.Tasks;
using CodeSmith.Core.Extensions;
using CodeSmith.Core.Scheduler;
using Exceptionless.Core.AppStats;
using Exceptionless.Core.Extensions;
using Exceptionless.Core.Queues;
using Exceptionless.Core.Queues.Models;
using Exceptionless.Core.Repositories;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using NLog.Fluent;

namespace Exceptionless.Core.Jobs {
    public class ProcessEventUserDescriptionsJob : Job {
        private readonly IQueue<EventUserDescription> _queue;
        private readonly IEventRepository _eventRepository;
        private readonly IAppStatsClient _statsClient;

        public ProcessEventUserDescriptionsJob(IQueue<EventUserDescription> queue, IEventRepository eventRepository, IAppStatsClient statsClient) {
            _queue = queue;
            _eventRepository = eventRepository;
            _statsClient = statsClient;
        }

        public void Run(int totalUserDescriptionsToProcess) {
            var context = new JobRunContext();
            context.Properties.Add("TotalUserDescriptionsToProcess", totalUserDescriptionsToProcess);
            Run(context);
        }

        public async override Task<JobResult> RunAsync(JobRunContext context) {
            Log.Info().Message("Process email message job starting").Write();
            int totalUserDescriptionsProcessed = 0;
            int totalUserDescriptionsToProcess = -1;
            if (context.Properties.ContainsKey("TotalUserDescriptionsToProcess"))
                totalUserDescriptionsToProcess = (int)context.Properties["TotalUserDescriptionsToProcess"];

            while (!CancelPending && (totalUserDescriptionsToProcess == -1 || totalUserDescriptionsProcessed < totalUserDescriptionsToProcess)) {
                QueueEntry<EventUserDescription> queueEntry = null;
                try {
                    queueEntry = await _queue.DequeueAsync();
                } catch (Exception ex) {
                    if (!(ex is TimeoutException)) {
                        Log.Error().Exception(ex).Message("An error occurred while trying to dequeue the next EventUserDescription: {0}", ex.Message).Write();
                        return JobResult.FromException(ex);
                    }
                }
                if (queueEntry == null)
                    continue;
                
                // TODO: Should we validate the dequeued item?
                _statsClient.Counter(StatNames.EventsUserDescriptionDequeued);

                Log.Info().Message("Processing EventUserDescription '{0}'.", queueEntry.Id).Write();

                try {
                    ProcessUserDescription(queueEntry.Value);
                    totalUserDescriptionsProcessed++;
                    _statsClient.Counter(StatNames.EventsUserDescriptionProcessed);
                } catch (Exception ex) {
                    _statsClient.Counter(StatNames.EventsUserDescriptionErrors);
                    queueEntry.AbandonAsync().Wait();

                    // TODO: Add the EventUserDescription to the logged exception.
                    Log.Error().Exception(ex).Message("An error occurred while processing the EventUserDescription '{0}': {1}", queueEntry.Id, ex.Message).Write();
                    return JobResult.FromException(ex);
                }

                await queueEntry.CompleteAsync();
            }

            return JobResult.Success;
        }

        private void ProcessUserDescription(EventUserDescription description) {
            var ev = _eventRepository.GetByReferenceId(description.ProjectId, description.ReferenceId).FirstOrDefault();
            if (ev == null)
                throw new InvalidOperationException("An event with this reference id has not been processed yet or was deleted.");

            // TODO: Should this be storing it in json?
            var ud = new UserDescription {
                EmailAddress = description.EmailAddress,
                Description = description.Description
            };
            if (description.Data.Count > 0)
                ev.Data.AddRange(description.Data);

            ev.SetUserDescription(ud);

            _eventRepository.Save(ev);
        }
    }
}