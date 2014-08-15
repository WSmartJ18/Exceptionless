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
using System.Collections.ObjectModel;

namespace Exceptionless.Models {
    public class Organization : IIdentity, IData, IOwnedByOrganization {
        public Organization() {
            Invites = new Collection<Invite>();
            BillingStatus = BillingStatus.Trialing;
            Usage = new Collection<UsageInfo>();
            OverageHours = new Collection<UsageInfo>();
            Data = new DataDictionary();
        }

        /// <summary>
        /// Unique id that identifies the organization.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the organization.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Stripe customer id that will be charged.
        /// </summary>
        public string StripeCustomerId { get; set; }

        /// <summary>
        /// Billing plan that the organization belongs to.
        /// </summary>
        public string PlanId { get; set; }

        /// <summary>
        /// Last 4 digits of the credit card used for billing.
        /// </summary>
        public string CardLast4 { get; set; }

        /// <summary>
        /// Date the organization first subscribed to a paid plan.
        /// </summary>
        public DateTime? SubscribeDate { get; set; }

        /// <summary>
        /// Date the billing information was last changed.
        /// </summary>
        public DateTime? BillingChangeDate { get; set; }

        /// <summary>
        /// User id that the billing information was last changed by.
        /// </summary>
        public string BillingChangedByUserId { get; set; }

        /// <summary>
        /// Organization's current billing status.
        /// </summary>
        public BillingStatus BillingStatus { get; set; }

        /// <summary>
        /// The price of the plan that this organization is currently on.
        /// </summary>
        public decimal BillingPrice { get; set; }

        /// <summary>
        /// Maximum number of event occurrences allowed per month.
        /// </summary>
        public int MaxEventsPerMonth { get; set; }

        /// <summary>
        /// Number of days stats data is retained.
        /// </summary>
        public int RetentionDays { get; set; }

        /// <summary>
        /// If true, the account is suspended and can't be used.
        /// </summary>
        public bool IsSuspended { get; set; }

        /// <summary>
        /// The code indicating why the account was suspended.
        /// </summary>
        public SuspensionCode? SuspensionCode { get; set; }

        /// <summary>
        /// Any notes on why the account was suspended.
        /// </summary>
        public string SuspensionNotes { get; set; }

        /// <summary>
        /// The reason the account was suspended.
        /// </summary>
        public DateTime? SuspensionDate { get; set; }

        /// <summary>
        /// User id that the suspended the account.
        /// </summary>
        public string SuspendedByUserId { get; set; }

        /// <summary>
        /// If true, premium features will be enabled.
        /// </summary>
        public bool HasPremiumFeatures { get; set; }

        /// <summary>
        /// Maximum number of users allowed by the current plan.
        /// </summary>
        public long MaxUsers { get; set; }

        /// <summary>
        /// Maximum number of projects allowed by the current plan.
        /// </summary>
        public int MaxProjects { get; set; }

        /// <summary>
        /// The date that the latest event occurred.
        /// </summary>
        public DateTime LastEventDate { get; set; }

        /// <summary>
        /// Total events logged by our system.
        /// </summary>
        public long TotalEventCount { get; set; }

        /// <summary>
        /// Organization invites.
        /// </summary>
        public ICollection<Invite> Invites { get; set; }

        /// <summary>
        /// Hours over event limit.
        /// </summary>
        public ICollection<UsageInfo> OverageHours { get; set; }

        /// <summary>
        /// Account event usage information.
        /// </summary>
        public ICollection<UsageInfo> Usage { get; set; }

        /// <summary>
        /// Optional data entries that contain additional configuration information for this organization.
        /// </summary>
        public DataDictionary Data { get; set; }

        string IOwnedByOrganization.OrganizationId { get { return Id; } set { Id = value; } }
    }

    public class UsageInfo {
        public DateTime Date { get; set; }
        public int Total { get; set; }
        public int Blocked { get; set; }
        public int Limit { get; set; }
    }

    public enum BillingStatus {
        Trialing = 0,
        Active = 1,
        PastDue = 2,
        Canceled = 3,
        Unpaid = 4
    }
}