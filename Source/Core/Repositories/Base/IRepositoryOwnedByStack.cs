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
using System.Threading.Tasks;
using Exceptionless.Models;

namespace Exceptionless.Core.Repositories {
    public interface IRepositoryOwnedByStack<T> : IRepository<T> where T : class, IOwnedByStack, IIdentity, new() {
        ICollection<T> GetByStackId(string stackId, PagingOptions paging = null, bool useCache = false, TimeSpan? expiresIn = null);
        Task RemoveAllByStackIdAsync(string stackId);
    }

    public interface IRepositoryOwnedByProjectAndStack<T> : IRepositoryOwnedByProject<T>, IRepositoryOwnedByStack<T> where T : class, IOwnedByProject, IIdentity, IOwnedByStack, new() { }

    public interface IRepositoryOwnedByOrganizationAndProjectAndStack<T> : IRepositoryOwnedByOrganization<T>, IRepositoryOwnedByProject<T>, IRepositoryOwnedByStack<T> where T : class, IOwnedByOrganization, IOwnedByProject, IIdentity, IOwnedByStack, new() { }
}