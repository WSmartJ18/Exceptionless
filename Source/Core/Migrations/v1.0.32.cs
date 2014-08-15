﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;
using Exceptionless.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations;

namespace Exceptionless.Core.Migrations {
    public class OrganizationConversionMigration : CollectionMigration {
        public OrganizationConversionMigration(): base("1.0.32", "organization") {
            Description = "Rename various organization fields";
        }

        public override void UpdateDocument(MongoCollection<BsonDocument> collection, BsonDocument document) {
            if (document.Contains("ErrorCount"))
                document.ChangeName("ErrorCount", "EventCount");

            if (document.Contains("TotalErrorCount"))
                document.ChangeName("TotalErrorCount", "TotalEventCount");

            if (document.Contains("LastErrorDate"))
                document.ChangeName("LastErrorDate", "LastEventDate");

            if (document.Contains("MaxErrorsPerMonth"))
                document.ChangeName("MaxErrorsPerMonth", "MaxEventsPerMonth");

            if (document.Contains("SuspensionCode")) {
                var value = document.GetValue("SuspensionCode");
                document.Remove("SuspensionCode");

                SuspensionCode suspensionCode;
                if (value.IsString && Enum.TryParse(value.AsString, true, out suspensionCode))
                    document.Set("SuspensionCode", suspensionCode);
            }

            collection.Save(document);
        }
    }
}