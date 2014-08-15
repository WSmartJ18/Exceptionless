﻿using System;
using CodeSmith.Core.Component;
using CodeSmith.Core.Extensions;
using Exceptionless.Core.Models;
using Exceptionless.Models;

namespace Exceptionless.Core.Plugins.Formatting {
    [Priority(50)]
    public class SessionFormattingPlugin : FormattingPluginBase {
        private bool ShouldHandle(PersistentEvent ev) {
            return ev.IsSessionStart() || ev.IsSessionEnd();
        }
        
        public SummaryData GetStackSummaryData(Stack stack) {
            if (!stack.SignatureInfo.ContainsKeyWithValue("Type", Event.KnownTypes.SessionStart, Event.KnownTypes.SessionEnd))
                return null;

            return new SummaryData("stack-session-summary", new { Title = stack.Title });
        }

        public override string GetStackTitle(PersistentEvent ev) {
            if (!ShouldHandle(ev))
                return null;

            return ev.IsSessionStart() ? "Session Starts" : "Session Ends";
        }

        public override SummaryData GetEventSummaryData(PersistentEvent ev) {
            if (!ShouldHandle(ev))
                return null;

            return new SummaryData("event-session-summary", new { SessionId = ev.SessionId });
        }

        public override string GetEventViewName(PersistentEvent ev) {
            if (!ShouldHandle(ev))
                return null;

            return "Event-Session";
        }
    }
}