﻿using System;
using CodeSmith.Core.Component;
using CodeSmith.Core.Extensions;
using Exceptionless.Core.Extensions;
using Exceptionless.Core.Mail.Models;
using Exceptionless.Core.Models;
using Exceptionless.Core.Queues.Models;
using Exceptionless.Extensions;
using Exceptionless.Models;
using RazorSharpEmail;

namespace Exceptionless.Core.Plugins.Formatting {
    [Priority(99)]
    public class DefaultFormattingPlugin : IFormattingPlugin {
        private readonly IEmailGenerator _emailGenerator;

        public DefaultFormattingPlugin(IEmailGenerator emailGenerator) {
            _emailGenerator = emailGenerator;
        }

        public string GetStackTitle(PersistentEvent ev) {
            return ev.Message;
        }

        public SummaryData GetStackSummaryData(Stack stack) {
            return new SummaryData("stack-summary");
        }

        public SummaryData GetEventSummaryData(PersistentEvent ev) {
            return new SummaryData("event-summary", new { Message = ev.Message });
        }

        public MailMessage GetEventNotificationMailMessage(EventNotification model) {
            if (String.IsNullOrEmpty(model.Event.Message))
                return null;

            string notificationType = "Occurrence event";
            if (model.IsNew)
                notificationType = "New event";
            else if (model.IsRegression)
                notificationType = "Regression event";

            if (model.IsCritical)
                notificationType = String.Concat("Critical ", notificationType.ToLower());

            var requestInfo = model.Event.GetRequestInfo();
            var mailerModel = new EventNotificationModel(model) {
                BaseUrl = Settings.Current.BaseURL,
                Subject = String.Concat(notificationType, ": ", model.Event.Message.Truncate(120)),
                Message =  model.Event.Message,
                Url = requestInfo != null ? requestInfo.GetFullPath(true, true, true) : null
            };

            return _emailGenerator.GenerateMessage(mailerModel, "Notice").ToMailMessage();
        }

        public string GetEventViewName(PersistentEvent ev) {
            return "Event";
        }
    }
}