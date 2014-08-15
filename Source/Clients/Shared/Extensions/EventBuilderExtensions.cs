﻿using System;

namespace Exceptionless {
    public static class EventBuilderExtensions {
        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) that the event happened to.
        /// </summary>
        /// <param name="builder">The event builder object.</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        public static EventBuilder SetUserIdentity(this EventBuilder builder, string identity) {
            builder.Target.SetUserIdentity(identity);
            return builder;
        }

        /// <summary>
        /// Sets the user's description of the event.
        /// </summary>
        /// <param name="builder">The event builder object.</param>
        /// <param name="emailAddress">The user's email address.</param>
        /// <param name="description">The user's description of the event.</param>
        public static EventBuilder SetUserDescription(this EventBuilder builder, string emailAddress, string description) {
            builder.Target.SetUserDescription(emailAddress, description);
            return builder;
        }
    }
}
