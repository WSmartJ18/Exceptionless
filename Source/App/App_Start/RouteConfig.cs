﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;
using System.Web.Mvc;
using System.Web.Routing;
using Exceptionless.App.Controllers;
using Exceptionless.App.Extensions;

namespace Exceptionless.App {
    public static class RouteConfig {
        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

#if DEBUG
            routes.IgnoreRoute("{*browserlink}", new { browserlink = @".*/arterySignalR/ping" });
#endif

            routes.MapRouteLowercase("Sign Up", "signup", new { controller = "Account", action = "Signup" });

            routes.MapRouteLowercase("Error Notifications", 
                "error/{stackId}/{errorId}", 
                new { controller = "Error", action = "Notification" }, 
                new { stackId = @"^[a-zA-Z\d]{24}$", errorId = @"^[a-zA-Z\d]{24}$" });

            routes.MapRouteLowercase("Middle Id", "{controller}/{id}/{action}", new { action = "Index" }, new { id = @"^[a-zA-Z\d]{24}$" });

            routes.MapRouteLowercase("Root", 
                "{action}/{id}",
                new { controller = "Home", id = UrlParameter.Optional }, 
                new { action = new IsControllerActionNameConstraint("Home") });

            routes.MapRouteLowercase("Default", "{controller}/{action}/{id}", new { controller = "Home", action = "Index", id = UrlParameter.Optional });
        }
    }
}