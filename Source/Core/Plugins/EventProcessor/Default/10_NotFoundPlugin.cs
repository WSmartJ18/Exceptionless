﻿using System;
using CodeSmith.Core.Component;
using Exceptionless.Extensions;
using Exceptionless.Models;

namespace Exceptionless.Core.Plugins.EventProcessor {
    [Priority(10)]
    public class NotFoundPlugin : EventProcessorPluginBase {
        public override void EventProcessing(EventContext context) {
            if (context.Event.Type != Event.KnownTypes.NotFound)
                return;

            context.Event.Data.Remove(Event.KnownDataKeys.EnvironmentInfo);
            context.Event.Data.Remove(Event.KnownDataKeys.TraceLog);

            var req = context.Event.GetRequestInfo();
            if (req == null)
                return;

            if (String.IsNullOrWhiteSpace(context.Event.Source)) {
                context.Event.Message = null;
                context.Event.Source = req.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
            }

            context.Event.Data.Remove(Event.KnownDataKeys.Error);
            context.Event.Data.Remove(Event.KnownDataKeys.SimpleError);
        }
    }
}