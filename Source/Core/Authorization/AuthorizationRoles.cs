﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;

namespace Exceptionless.Core.Authorization {
    public static class AuthorizationRoles {
        public const string Client = "client";
        public const string User = "user";
        public const string GlobalAdmin = "global";
        public const string UserOrClient = "user,client";
        public static readonly string[] All = { Client, User };
        public static readonly string[] GlobalAll = { Client, User, GlobalAdmin };
    }
}