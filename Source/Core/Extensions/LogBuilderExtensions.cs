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
using Exceptionless;

namespace NLog.Fluent {
    public static class LogBuilderExtensions {
        public static LogBuilder Report(this LogBuilder builder, Action<EventBuilder> errorBuilderAction = null) {
            if (builder.LogEventInfo.Exception != null) {
                EventBuilder exBuilder = builder.LogEventInfo.Exception.ToExceptionless();

                if (errorBuilderAction != null)
                    errorBuilderAction(exBuilder);

                exBuilder.AddObject(builder.LogEventInfo, name: "Log Info", excludedPropertyNames: new List<string> { "Exception", "Message", "LoggerShortName", "SequenceID", "TimeStamp" });

                exBuilder.Submit();
            }

            return builder;
        }

        public static LogBuilder Project(this LogBuilder builder, string projectId) {
            return builder.Property("project", projectId);
        }
    }
}