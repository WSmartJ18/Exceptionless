﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeSmith.Core.Scheduler;
using Exceptionless.Core.AppStats;
using Exceptionless.Core.Plugins.EventParser;
using Exceptionless.Core.Extensions;
using Exceptionless.Core.Models;
using Exceptionless.Core.Pipeline;
using Exceptionless.Core.Queues;
using Exceptionless.Core.Repositories;
using Exceptionless.Models;
using FluentValidation;
using NLog.Fluent;

namespace Exceptionless.Core.Jobs {
    public class ProcessEventPostsJob : Job {
        private readonly IQueue<EventPost> _queue;
        private readonly EventParserPluginManager _eventParserPluginManager;
        private readonly EventPipeline _eventPipeline;
        private readonly IAppStatsClient _statsClient;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IProjectRepository _projectRepository;

        public ProcessEventPostsJob(IQueue<EventPost> queue, EventParserPluginManager eventParserPluginManager, EventPipeline eventPipeline, IAppStatsClient statsClient, IOrganizationRepository organizationRepository, IProjectRepository projectRepository) {
            _queue = queue;
            _eventParserPluginManager = eventParserPluginManager;
            _eventPipeline = eventPipeline;
            _statsClient = statsClient;
            _organizationRepository = organizationRepository;
            _projectRepository = projectRepository;
        }

        public void Run(int totalEventsToProcess) {
            var context = new JobRunContext();
            context.Properties.Add("TotalEventsToProcess", totalEventsToProcess);
            Run(context);
        }

        public async override Task<JobResult> RunAsync(JobRunContext context) {
            Log.Info().Message("Process events job starting").Write();
            int totalEventsProcessed = 0;
            int totalEventsToProcess = -1;
            if (context.Properties.ContainsKey("TotalEventsToProcess"))
                totalEventsToProcess = (int)context.Properties["TotalEventsToProcess"];

            while (!CancelPending && (totalEventsToProcess == -1 || totalEventsProcessed < totalEventsToProcess)) {
                QueueEntry<EventPost> queueEntry = null;
                try {
                    queueEntry = await _queue.DequeueAsync();
                } catch (Exception ex) {
                    if (!(ex is TimeoutException)) {
                        Log.Error().Exception(ex).Message("An error occurred while trying to dequeue the next EventPost: {0}", ex.Message).Write();
                        return JobResult.FromException(ex);
                    }
                }
                if (queueEntry == null)
                    continue;
  
                _statsClient.Counter(StatNames.PostsDequeued);
                Log.Info().Message("Processing EventPost '{0}'.", queueEntry.Id).Write();
                
                List<PersistentEvent> events = null;
                try {
                    _statsClient.Time(() => {
                        events = ParseEventPost(queueEntry.Value);
                    }, StatNames.PostsParsingTime);
                    _statsClient.Counter(StatNames.PostsParsed);
                    _statsClient.Gauge(StatNames.PostsBatchSize, events.Count);
                } catch (Exception ex) {
                    _statsClient.Counter(StatNames.PostsParseErrors);
                    queueEntry.AbandonAsync().Wait();

                    // TODO: Add the EventPost to the logged exception.
                    Log.Error().Exception(ex).Message("An error occurred while processing the EventPost '{0}': {1}", queueEntry.Id, ex.Message).Write();
                    continue;
                }
       
                if (events == null) {
                    queueEntry.AbandonAsync().Wait();
                    continue;
                }

                int eventsToProcess = events.Count;
                bool isSingleEvent = events.Count == 1;
                if (!isSingleEvent) {
                    var project = _projectRepository.GetById(queueEntry.Value.ProjectId, true);
                    // Don't process all the events if it will put the account over its limits.
                    eventsToProcess = _organizationRepository.GetRemainingEventLimit(project.OrganizationId);

                    // Add 1 because we already counted 1 against their limit when we received the event post.
                    if (eventsToProcess < Int32.MaxValue)
                        eventsToProcess += 1;

                    // Increment by count - 1 since we already incremented it by 1 in the OverageHandler.
                    _organizationRepository.IncrementUsage(project.OrganizationId, events.Count - 1);
                }
                int errorCount = 0;
                foreach (PersistentEvent ev in events.Take(eventsToProcess)) {
                    try {
                        _eventPipeline.Run(ev);
                        totalEventsProcessed++;
                        if (totalEventsToProcess > 0 && totalEventsProcessed >= totalEventsToProcess)
                            break;
                    } catch (ValidationException ex) {
                        Log.Error().Exception(ex).Project(queueEntry.Value.ProjectId).Message("Event validation error occurred: {0}", ex.Message).Write();
                    } catch (Exception ex) {
                        Log.Error().Exception(ex).Project(queueEntry.Value.ProjectId).Message("Error while processing event: {0}", ex.Message).Write();

                        if (!isSingleEvent) {
                            // Put this single event back into the queue so we can retry it separately.
                            _queue.EnqueueAsync(new EventPost {
                                Data = Encoding.UTF8.GetBytes(ev.ToJson()).Compress(),
                                ContentEncoding = "gzip",
                                ProjectId = ev.ProjectId,
                                CharSet = "utf-8",
                                MediaType = "application/json",
                            }).Wait();
                        }

                        errorCount++;
                    }
                }

                if (isSingleEvent && errorCount > 0)
                    queueEntry.AbandonAsync().Wait();
                else
                    queueEntry.CompleteAsync().Wait();
            }

            return JobResult.Success;
        }

        private List<PersistentEvent> ParseEventPost(EventPost ep) {
            byte[] data = ep.Data;
            if (!String.IsNullOrEmpty(ep.ContentEncoding))
                data = data.Decompress(ep.ContentEncoding);

            var encoding = Encoding.UTF8;
            if (!String.IsNullOrEmpty(ep.CharSet))
                encoding = Encoding.GetEncoding(ep.CharSet);

            string input = encoding.GetString(data);
            List<PersistentEvent> events = _eventParserPluginManager.ParseEvents(input, ep.ApiVersion, ep.UserAgent);
            events.ForEach(e => {
                // set the project id on all events
                e.ProjectId = ep.ProjectId;
            });

            return events;
        }
    }
}