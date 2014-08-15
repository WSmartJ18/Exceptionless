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
using Exceptionless.Models.Admin;

namespace Exceptionless.Core.Repositories {
    public interface IWebHookRepository : IRepositoryOwnedByOrganizationAndProject<WebHook> {
        void RemoveByUrl(string targetUrl);
        ICollection<WebHook> GetByOrganizationIdOrProjectId(string organizationId, string projectId);
    }
}