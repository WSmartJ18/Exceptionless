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
using CodeSmith.Core.Extensions;
using CodeSmith.Core.Helpers;
using Exceptionless.Core.Authorization;
using Exceptionless.Models;
using MongoDB.Bson;

namespace Exceptionless.Tests.Utility {
    internal static class UserData {
        public static IEnumerable<User> GenerateUsers(int count = 10, bool generateId = false, string id = null, string organizationId = null, string emailAddress = null, List<string> roles = null) {
            for (int i = 0; i < count; i++)
                yield return GenerateUser(generateId, id, organizationId, emailAddress, roles);
        }

        public static IEnumerable<User> GenerateSampleUsers() {
            return new List<User> {
                GenerateSampleUser(),
                GenerateSampleUserWithNoRoles(),
                GenerateUser(id: TestConstants.UserId2, organizationId: TestConstants.OrganizationId2, emailAddress: TestConstants.UserEmail2, roles: new List<string> {
                    AuthorizationRoles.GlobalAdmin,
                    AuthorizationRoles.User,
                    AuthorizationRoles.Client
                })
            };
        }

        public static User GenerateSampleUser() {
            return GenerateUser(id: TestConstants.UserId, organizationId: TestConstants.OrganizationId, emailAddress: TestConstants.UserEmail, roles: new List<string> {
                AuthorizationRoles.GlobalAdmin,
                AuthorizationRoles.User,
                AuthorizationRoles.Client
            });
        }

        public static User GenerateSampleUserWithNoRoles() {
            return GenerateUser(id: TestConstants.UserIdWithNoRoles, organizationId: TestConstants.OrganizationId, emailAddress: TestConstants.UserEmailWithNoRoles);
        }

        public static User GenerateUser(bool generateId = false, string id = null, string organizationId = null, string emailAddress = null, IEnumerable<string> roles = null) {
            var user = new User {
                Id = id.IsNullOrEmpty() ? generateId ? ObjectId.GenerateNewId().ToString() : TestConstants.UserId : id,
                EmailAddress = emailAddress.IsNullOrEmpty() ? String.Concat(RandomHelper.GetPronouncableString(6), "@", RandomHelper.GetPronouncableString(6), ".com") : emailAddress,
                Password = TestConstants.UserPassword,
                FullName = "Eric Smith",
                PasswordResetToken = Guid.NewGuid().ToString()
            };

            user.OrganizationIds.Add(organizationId.IsNullOrEmpty() ? TestConstants.OrganizationId : organizationId);

            if (roles != null)
                user.Roles.AddRange(roles);

            return user;
        }
    }
}