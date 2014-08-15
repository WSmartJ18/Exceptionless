﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CodeSmith.Core.Extensions;

namespace Exceptionless.EventMigration.Models.Collections {
    public class ModuleCollection : Collection<Module> {
        public Exceptionless.Models.ModuleCollection ToModules() {
            var modules = new Exceptionless.Models.ModuleCollection();
            modules.AddRange(Items.Select(i => i.ToModule()));
            return modules;
        }
    }
}