﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;
using CodeSmith.Core.Component;
using Exceptionless.Core.Plugins.EventProcessor;
using Exceptionless.Core.Repositories;
using NLog.Fluent;

namespace Exceptionless.Core.Pipeline {
    [Priority(30)]
    public class CheckForRegressionAction : EventPipelineActionBase {
        private readonly IStackRepository _stackRepository;
        private readonly IEventRepository _eventRepository;

        public CheckForRegressionAction(IStackRepository stackRepository, IEventRepository eventRepository) {
            _stackRepository = stackRepository;
            _eventRepository = eventRepository;
        }

        protected override bool ContinueOnError { get { return true; } }

        public override void Process(EventContext ctx) {
            if (ctx.StackInfo == null || !ctx.StackInfo.DateFixed.HasValue || ctx.StackInfo.DateFixed.Value >= ctx.Event.Date.UtcDateTime)
                return;

            Log.Trace().Message("Marking event as an regression.").Write();
            _stackRepository.MarkAsRegressed(ctx.StackInfo.Id);
            _eventRepository.MarkAsRegressedByStack(ctx.StackInfo.Id);

            string signatureHash = ctx.GetProperty<string>("__SignatureHash");
            _stackRepository.InvalidateCache(ctx.Event.StackId, signatureHash, ctx.Event.ProjectId);

            ctx.Event.IsFixed = false;
            ctx.IsRegression = true;
        }
    }
}