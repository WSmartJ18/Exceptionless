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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations;

namespace Exceptionless.Core.Migrations {
    public class ProjectConversionMigration : CollectionMigration {
        public ProjectConversionMigration() : base("1.0.31", "project") {
            Description = "Migrate ApiKeys to the token repository and rename various project fields.";
        }

        public override void Update() {
            var projectCollection = GetCollection();
            if (projectCollection.IndexExists("ApiKeys"))
                projectCollection.DropIndex("ApiKeys");

            base.Update();
        }

        public override void UpdateDocument(MongoCollection<BsonDocument> collection, BsonDocument document) {
            if (document.Contains("ErrorCount"))
                document.ChangeName("ErrorCount", "EventCount");

            if (document.Contains("TotalErrorCount"))
                document.ChangeName("TotalErrorCount", "TotalEventCount");

            if (document.Contains("LastErrorDate"))
                document.ChangeName("LastErrorDate", "LastEventDate");

            BsonValue keys;
            if (document.TryGetValue("ApiKeys", out keys) && keys.IsBsonArray) {
                if (!collection.Database.CollectionExists("token"))
                    collection.Database.CreateCollection("token");

                var projectId = document.GetValue("_id").AsObjectId;

                var tokenCollection = Database.GetCollection("token");
                var tokens = new List<BsonDocument>();
                foreach (var key in keys.AsBsonArray) {
                    var token = new BsonDocument();
                    token.Set("_id", key);
                    token.Set("oid", new BsonObjectId(document.GetValue("oid").AsObjectId));
                    token.Set("pid", new BsonObjectId(projectId));
                    token.Set("typ", TokenType.Access);
                    token.Set("scp", new BsonArray(new[] { "client" }));
                    token.Set("exp", DateTime.UtcNow.AddYears(100));
                    token.Set("not", "Client api key");
                    token.Set("dt", projectId.CreationTime.ToUniversalTime());
                    token.Set("mdt", DateTime.UtcNow);

                    tokens.Add(token);
                }

                if (tokens.Count > 0)
                    tokenCollection.InsertBatch(tokens);
            }

            document.Remove("ApiKeys");

            collection.Save(document);
        }
    }
}