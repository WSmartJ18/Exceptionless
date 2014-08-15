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
using System.Diagnostics;
using System.Linq;
using CodeSmith.Core.Extensions;
using Exceptionless.Core;
using Exceptionless.Core.Pipeline;
using Exceptionless.Core.Repositories;
using Exceptionless.Core.Utility;
using Exceptionless.Membership;
using Exceptionless.Models;
using Exceptionless.Tests.Utility;
using Xunit;

namespace Exceptionless.Tests.Analytics {
    public class StackStatsTests : MongoRepositoryTestBaseWithIdentity<Event, IEventRepository> {
        private readonly DataHelper _dataHelper = IoC.GetInstance<DataHelper>();
        private readonly EventStatsHelper _eventStatsHelper = IoC.GetInstance<EventStatsHelper>();
        private readonly IProjectRepository _projectRepository = IoC.GetInstance<IProjectRepository>();
        private readonly IOrganizationRepository _organizationRepository = IoC.GetInstance<IOrganizationRepository>();
        private readonly IUserRepository _userRepository = IoC.GetInstance<IUserRepository>();
        private readonly IStackRepository _stackRepository = IoC.GetInstance<IStackRepository>();

        private readonly IDayStackStatsRepository _dayStackStats = IoC.GetInstance<IDayStackStatsRepository>();
        private readonly IMonthStackStatsRepository _monthStackStats = IoC.GetInstance<IMonthStackStatsRepository>();
        private readonly IDayProjectStatsRepository _dayProjectStats = IoC.GetInstance<IDayProjectStatsRepository>();
        private readonly IMonthProjectStatsRepository _monthProjectStats = IoC.GetInstance<IMonthProjectStatsRepository>();

        private readonly EventPipeline _eventPipeline = IoC.GetInstance<EventPipeline>();

        public StackStatsTests() : base(IoC.GetInstance<IEventRepository>(), true) {}

        [Fact]
        public void CanGetMinuteStats() {
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            DateTime startDate = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Unspecified).Add(timeOffset);
            DateTime utcStartDate = new DateTimeOffset(startDate, timeOffset).UtcDateTime;
            DateTime endDate = startDate.AddDays(2);
            DateTime utcEndDate = new DateTimeOffset(endDate, timeOffset).UtcDateTime;
            const int count = 100;

            var events = EventData.GenerateEvents(count, startDate: startDate, endDate: endDate, stackId: TestConstants.StackId, projectId: TestConstants.ProjectId, timeZoneOffset: timeOffset).ToList();
            DateTimeOffset first = events.Min(e => e.Date);
            Assert.True(first >= utcStartDate);
            DateTimeOffset last = events.Max(e => e.Date);
            Assert.True(last <= utcEndDate);
            _eventPipeline.Run(events);

            var info = _eventStatsHelper.GetProjectEventStatsByMinuteBlock(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(count, info.Total);
            Assert.Equal(1, info.UniqueTotal);
            Assert.Equal(0, info.NewTotal);
            //Assert.Equal(1, info.Stats.Count);
            Assert.Equal(count, info.Stats.Sum(ds => ds.Total));
            Assert.True(info.Stats.All(ds => ds.UniqueTotal <= 1));
            Assert.Equal(0, info.Stats.Sum(ds => ds.NewTotal));
        }

        [Fact]
        public void CanAggregateStatsOverSmallTime() {
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            DateTime startDate = DateTime.UtcNow.Add(timeOffset).Date;
            DateTime endDate = DateTime.UtcNow.Add(timeOffset).Date.AddMinutes(5);
            const int count = 25;

            var events = EventData.GenerateEvents(count, startDate: startDate, endDate: endDate, stackId: TestConstants.StackId, projectId: TestConstants.ProjectId, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events);

            var info = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(count, info.Total);
            Assert.Equal(1, info.UniqueTotal);
            Assert.Equal(0, info.NewTotal);
            //Assert.Equal(1, info.Stats.Count);
            Assert.Equal(count, info.Stats.Sum(ds => ds.Total));
            Assert.True(info.Stats.All(ds => ds.UniqueTotal <= 1));
            Assert.Equal(0, info.Stats.Sum(ds => ds.NewTotal));
        }

        [Fact]
        public void CanAggregateStatsOverTwoMonths() {
            _dataHelper.ResetProjectData(TestConstants.ProjectId);
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            var overallsw = new Stopwatch();
            var sw = new Stopwatch();
            DateTime startDate = DateTime.UtcNow.Add(timeOffset).Date.AddMonths(-2);
            DateTime endDate = DateTime.UtcNow.Add(timeOffset).Date;
            const int count = 100;

            overallsw.Start();
            sw.Start();
            var events = EventData.GenerateEvents(count, organizationId: TestConstants.OrganizationId, projectId: TestConstants.ProjectId, startDate: startDate, endDate: endDate, stackId: TestConstants.StackId, timeZoneOffset: timeOffset).ToList();
            sw.Stop();
            Console.WriteLine("Generate Events: {0}", sw.Elapsed.ToWords(true));

            sw.Restart();
            _eventPipeline.Run(events);
            sw.Stop();
            Console.WriteLine("Add Events: {0}", sw.Elapsed.ToWords(true));

            sw.Restart();
            var info = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            sw.Stop();
            Console.WriteLine("Get Stats: {0}", sw.Elapsed.ToWords(true));
            overallsw.Stop();
            Console.WriteLine("Overall: {0}", overallsw.Elapsed.ToWords(true));

            Assert.Equal(count, info.Total);
            Assert.InRange(info.UniqueTotal, 1, count);
            Assert.Equal(0, info.NewTotal);
            Assert.Equal(info.EndDate.Subtract(info.StartDate).TotalDays + 1, info.Stats.Count);
            Assert.Equal(count, info.Stats.Sum(ds => ds.Total));
            Assert.Equal(0, info.Stats.Sum(ds => ds.NewTotal));
        }

        [Fact]
        public void CanAggregateStatsOverSeveralMonths() {
            _dataHelper.ResetProjectData(TestConstants.ProjectId);
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            DateTime startDate = DateTime.UtcNow.Add(timeOffset).Date.AddDays(-120);
            DateTime endDate = DateTime.UtcNow.Add(timeOffset).Date;
            const int count = 25;

            var events = EventData.GenerateEvents(count, organizationId: TestConstants.OrganizationId, startDate: startDate, endDate: endDate, stackId: TestConstants.StackId, projectId: TestConstants.ProjectId, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events);

            var info = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(count, info.Total);
            Assert.InRange(info.UniqueTotal, 1, count);
            Assert.Equal(0, info.NewTotal);
            Assert.True(info.Stats.Count > 40);
            Assert.Equal(count, info.Stats.Sum(ds => ds.Total));
            Assert.Equal(0, info.Stats.Sum(ds => ds.NewTotal));
        }

        [Fact]
        public void CanAggregateStatsOverSeveralMonthsForMultipleProjects() {
            _dataHelper.ResetProjectData(TestConstants.ProjectId);
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            DateTime startDate = DateTime.UtcNow.Add(timeOffset).Date.AddDays(-120);
            DateTime endDate = DateTime.UtcNow.Add(timeOffset).Date;
            const int count1 = 50;
            const int count2 = 50;

            var events1 = EventData.GenerateEvents(count1, organizationId: TestConstants.OrganizationId, startDate: startDate, endDate: endDate, stackId: TestConstants.StackId, projectId: TestConstants.ProjectId, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events1);

            var events2 = EventData.GenerateEvents(count2, organizationId: TestConstants.OrganizationId, startDate: startDate, endDate: endDate, stackId: TestConstants.StackId2, projectId: TestConstants.ProjectIdWithNoRoles, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events2);

            var info1 = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(count1, info1.Total);
            Assert.InRange(info1.UniqueTotal, 1, count1);
            Assert.Equal(0, info1.NewTotal);
            Assert.True(info1.Stats.Count > 40);
            Assert.Equal(count1, info1.Stats.Sum(ds => ds.Total));
            Assert.Equal(0, info1.Stats.Sum(ds => ds.NewTotal));

            var info2 = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectIdWithNoRoles, timeOffset, startDate, endDate);
            Assert.Equal(count2, info2.Total);
            Assert.InRange(info2.UniqueTotal, 1, count2);
            Assert.Equal(0, info2.NewTotal);
            Assert.True(info2.Stats.Count > 40);
            Assert.Equal(count2, info2.Stats.Sum(ds => ds.Total));
            Assert.Equal(0, info2.Stats.Sum(ds => ds.NewTotal));
        }

        [Fact]
        public void CanStackEvents() {
            _dataHelper.ResetProjectData(TestConstants.ProjectId);
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            DateTime startDate = DateTime.UtcNow.Add(timeOffset).Date.AddDays(-120);
            DateTime endDate = DateTime.UtcNow.Add(timeOffset).Date;
            const int count = 25;

            var events = EventData.GenerateEvents(count, organizationId: TestConstants.OrganizationId, startDate: startDate, endDate: endDate, projectId: TestConstants.ProjectId, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events);

            long stackCount = _stackRepository.Count();

            var info = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(count, info.Total);
            Assert.InRange(info.UniqueTotal, 1, count);
            Assert.Equal(stackCount, info.NewTotal);
            Assert.True(info.Stats.Count > 40);
            Assert.Equal(count, info.Stats.Sum(ds => ds.Total));
            Assert.Equal(stackCount, info.Stats.Sum(ds => ds.NewTotal));
        }

        [Fact]
        public void CanStackEventsForMultipleProjects() {
            _dataHelper.ResetProjectData(TestConstants.ProjectId);
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            DateTime startDate = DateTime.UtcNow.Add(timeOffset).Date.AddDays(-120);
            DateTime endDate = DateTime.UtcNow.Add(timeOffset).Date;
            const int count = 25;

            var events1 = EventData.GenerateEvents(count, organizationId: TestConstants.OrganizationId, startDate: startDate, endDate: endDate, projectId: TestConstants.ProjectId, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events1);

            var events2 = EventData.GenerateEvents(count, organizationId: TestConstants.OrganizationId, startDate: startDate, endDate: endDate, projectId: TestConstants.ProjectIdWithNoRoles, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events2);

            long stackCount = _stackRepository.Where(es => es.ProjectId == TestConstants.ProjectId).Count();

            var info = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(count, info.Total);
            Assert.InRange(info.UniqueTotal, 1, count);
            Assert.Equal(stackCount, info.NewTotal);
            Assert.True(info.Stats.Count > 40);
            Assert.Equal(count, info.Stats.Sum(ds => ds.Total));
            Assert.Equal(stackCount, info.Stats.Sum(ds => ds.NewTotal));
        }

        [Fact]
        public void CanCalculateTimeBuckets() {
            var bucket = EventStatsHelper.GetTimeBucket(new DateTimeOffset(2012, 11, 16, 0, 13, 43, TimeSpan.FromHours(-0)));
            Assert.Equal(0, bucket);

            bucket = EventStatsHelper.GetTimeBucket(new DateTimeOffset(2012, 11, 16, 0, 15, 43, TimeSpan.FromHours(-0)));
            Assert.Equal(15, bucket);

            bucket = EventStatsHelper.GetTimeBucket(new DateTimeOffset(2012, 11, 16, 23, 59, 59, TimeSpan.FromHours(-0)));
            Assert.Equal(1425, bucket);

            var buckets = new List<int>();
            for (int i = 0; i < 1440; i += 15)
                buckets.Add(i);

            Assert.Equal(96, buckets.Count);
        }

        [Fact]
        public void CanResetStackStats() {
            _dataHelper.ResetProjectData(TestConstants.ProjectId);
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            DateTime startDate = DateTime.UtcNow.Add(timeOffset).Date.AddDays(-45);
            DateTime endDate = DateTime.UtcNow.Add(timeOffset).Date;
            const int count = 100;

            var events1 = EventData.GenerateEvents(count, organizationId: TestConstants.OrganizationId, startDate: startDate, endDate: endDate, projectId: TestConstants.ProjectId, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events1);

            long stackCount = _stackRepository.Where(es => es.ProjectId == TestConstants.ProjectId).Count();
            var firstStack = _stackRepository.Where(es => es.ProjectId == TestConstants.ProjectId).OrderBy(es => es.FirstOccurrence).First();
            Console.WriteLine("Count: " + firstStack.TotalOccurrences);

            var info = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(count, info.Total);
            Assert.InRange(info.UniqueTotal, 1, count);
            Assert.Equal(stackCount, info.NewTotal);
            Assert.Equal(info.EndDate.Subtract(info.StartDate).TotalDays + 1, info.Stats.Count);
            Assert.Equal(count, info.Stats.Sum(ds => ds.Total));
            Assert.Equal(stackCount, info.Stats.Sum(ds => ds.NewTotal));

            _eventStatsHelper.DecrementDayProjectStatsByStackId(TestConstants.ProjectId, firstStack.Id);
            _eventStatsHelper.DecrementMonthProjectStatsByStackId(TestConstants.ProjectId, firstStack.Id);

            info = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(count - firstStack.TotalOccurrences, info.Total);
            Assert.InRange(info.UniqueTotal - 1, 1, count);
            Assert.Equal(stackCount - 1, info.NewTotal);
            Assert.Equal(info.EndDate.Subtract(info.StartDate).TotalDays + 1, info.Stats.Count);
            Assert.Equal(count - firstStack.TotalOccurrences, info.Stats.Sum(ds => ds.Total));
            Assert.Equal(stackCount - 1, info.Stats.Sum(ds => ds.NewTotal));
        }

        [Fact]
        public void CanHideStacksFromStats() {
            _dataHelper.ResetProjectData(TestConstants.ProjectId);
            TimeSpan timeOffset = _projectRepository.GetDefaultTimeOffset(TestConstants.ProjectId);
            DateTime startDate = DateTime.UtcNow.Add(timeOffset).Date.AddDays(-120);
            DateTime endDate = DateTime.UtcNow.Add(timeOffset).Date;
            const int count = 50;

            var events = EventData.GenerateEvents(count, organizationId: TestConstants.OrganizationId, startDate: startDate, endDate: endDate, projectId: TestConstants.ProjectId, timeZoneOffset: timeOffset).ToList();
            _eventPipeline.Run(events);

            var firstStack = _stackRepository.Where(es => es.ProjectId == TestConstants.ProjectId).OrderBy(es => es.FirstOccurrence).First();
            firstStack.IsHidden = true;
            _stackRepository.Update(firstStack);

            var biggestStack = _stackRepository.Where(es => es.ProjectId == TestConstants.ProjectId && !es.IsHidden).OrderByDescending(es => es.TotalOccurrences).First();
            biggestStack.IsHidden = true;
            _stackRepository.Update(biggestStack);
            _stackRepository.InvalidateHiddenIdsCache(TestConstants.ProjectId);

            long stackCount = _stackRepository.Where(s => !s.IsHidden).Count();
            int countWithoutHidden = count - firstStack.TotalOccurrences - biggestStack.TotalOccurrences;

            var info = _eventStatsHelper.GetProjectEventStats(TestConstants.ProjectId, timeOffset, startDate, endDate);
            Assert.Equal(countWithoutHidden, info.Total);
            Assert.InRange(info.UniqueTotal, 1, count);
            Assert.Equal(stackCount, info.NewTotal);
            Assert.True(info.Stats.Count > 40);
            Assert.Equal(countWithoutHidden, info.Stats.Sum(ds => ds.Total));
            Assert.Equal(stackCount, info.Stats.Sum(ds => ds.NewTotal));
        }

        protected override void CreateData() {
            var membershipProvider = new MembershipProvider(_userRepository.Collection);
            foreach (User user in UserData.GenerateSampleUsers())
                membershipProvider.CreateAccount(user);
            _projectRepository.Add(ProjectData.GenerateSampleProjects());
            _organizationRepository.Add(OrganizationData.GenerateSampleOrganizations());

            //_errorStackRepository.Add(EventStackData.GenerateEventStack(id: TestConstants.EventStackId, organizationId: TestConstants.OrganizationId, projectId: TestConstants.ProjectId));
            //_errorStackRepository.Add(EventStackData.GenerateEventStack(id: TestConstants.EventStackId2, organizationId: TestConstants.OrganizationId, projectId: TestConstants.ProjectIdWithNoRoles));
        }

        protected override void RemoveData() {
            base.RemoveData();
            _stackRepository.DeleteAll();
            _projectRepository.DeleteAll();
            _organizationRepository.DeleteAll();
            _userRepository.DeleteAll();

            _dayStackStats.DeleteAll();
            _monthStackStats.DeleteAll();
            _dayProjectStats.DeleteAll();
            _monthProjectStats.DeleteAll();
        }
    }
}