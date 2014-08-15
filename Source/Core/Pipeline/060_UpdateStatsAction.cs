﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;
using System.Collections.Generic;
using CodeSmith.Core.Component;
using Exceptionless.Core.Plugins.EventProcessor;
using Exceptionless.Core.Repositories;
using Exceptionless.Core.Utility;

namespace Exceptionless.Core.Pipeline {
    [Priority(60)]
    public class UpdateStatsAction : EventPipelineActionBase {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IStackRepository _stackRepository;
        private readonly EventStatsHelper _statsHelper;

        public UpdateStatsAction(EventStatsHelper statsHelper, IOrganizationRepository organizationRepository, IProjectRepository projectRepository, IStackRepository stackRepository) {
            _organizationRepository = organizationRepository;
            _projectRepository = projectRepository;
            _stackRepository = stackRepository;
            _statsHelper = statsHelper;
        }

        protected override bool IsCritical { get { return true; } }

        public override void Process(EventContext ctx) {
            _organizationRepository.IncrementEventCounter(ctx.Event.OrganizationId);
            _projectRepository.IncrementEventCounter(ctx.Event.ProjectId);
            if (!ctx.IsNew)
                _stackRepository.IncrementEventCounter(ctx.Event.StackId, ctx.Event.Date.UtcDateTime);
        }
    }
}