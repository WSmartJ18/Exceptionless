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

namespace Exceptionless.EventMigration.Models {
    public class StackFrameCollection : Collection<StackFrame> {
        public Exceptionless.Models.StackFrameCollection ToStackTrace() {
            var frames = new Exceptionless.Models.StackFrameCollection();
            foreach (var item in Items) {
                frames.Add(item.ToStackFrame());
            }

            return frames;
        }
    }
}