﻿using System;
using System.Collections.Generic;

namespace Exceptionless.Models.Admin {
    public class Token : IIdentity, IOwnedByOrganization, IOwnedByProject {
        public Token() {
            Scopes = new HashSet<string>();
        }

        public string Id { get; set; }
        public string OrganizationId { get; set; }
        public string ProjectId { get; set; }
        public string UserId { get; set; }
        public string ApplicationId { get; set; }
        public string DefaultProjectId { get; set; }
        public string Refresh { get; set; }
        public TokenType Type { get; set; }
        public HashSet<string> Scopes { get; set; }
        public DateTime? ExpiresUtc { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime ModifiedUtc { get; set; }
    }

    public enum TokenType {
        Authentication,
        Access
    }
}
