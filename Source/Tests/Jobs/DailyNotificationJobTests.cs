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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeSmith.Core.Scheduler;
using DotNetOpenAuth.Messaging;
using Exceptionless.Core.Jobs;
using Exceptionless.Core.Messaging;
using Exceptionless.Core.Queues.Models;
using Exceptionless.Core.Repositories;
using Exceptionless.Models;
using Exceptionless.Tests.Utility;
using Moq;
using Xunit;

namespace Exceptionless.Tests.Jobs {
    public class DailyNotificationJobTests : MongoRepositoryTestBase<Project, IProjectRepository> {
        private readonly List<SummaryNotification> _messages = new List<SummaryNotification>();
        private readonly Mock<IMessageFactory> _messageFactoryMock = new Mock<IMessageFactory>();
        private readonly Mock<IMessageSubscriber> _messageSubscriberMock = new Mock<IMessageSubscriber>();

        public DailyNotificationJobTests() : base(IoC.GetInstance<IProjectRepository>(), true) {}

        private const int OFFSET = -9;

        [Fact]
        public void CanQueueNotifications() {
            _messages.Clear();

            Project project = ProjectData.GenerateProject(generateId: true, name: "Project1", nextSummaryEndOfDayTicks: DateTime.UtcNow.AddHours(OFFSET).AddMinutes(-1).Ticks);
            Repository.Add(project);

            Project project2 = ProjectData.GenerateProject(generateId: true, name: "Project2", nextSummaryEndOfDayTicks: DateTime.UtcNow.AddHours(OFFSET).AddMilliseconds(250).Ticks);
            Repository.Add(project2);

            var job = new DailyNotificationJob(Repository, _messageFactoryMock.Object);
            job.Run(new JobRunContext("Daily Summary", "", DateTime.Now, JobStatus.None, null, null, null));

            Assert.Equal(1, _messages.Count);
            Assert.Equal(project.NextSummaryEndOfDayTicks + TimeSpan.TicksPerDay, Repository.GetById(project.Id).NextSummaryEndOfDayTicks);
            Assert.Equal(project2.NextSummaryEndOfDayTicks, Repository.GetById(project2.Id).NextSummaryEndOfDayTicks);

            job.Run(new JobRunContext("Daily Summary", "", DateTime.Now, JobStatus.None, null, null, null));

            Assert.Equal(1, _messages.Count);
            Assert.Equal(project.NextSummaryEndOfDayTicks + TimeSpan.TicksPerDay, Repository.GetById(project.Id).NextSummaryEndOfDayTicks);
            Assert.Equal(project2.NextSummaryEndOfDayTicks, Repository.GetById(project2.Id).NextSummaryEndOfDayTicks);

            Thread.Sleep(250);
            job.Run(new JobRunContext("Daily Summary", "", DateTime.Now, JobStatus.None, null, null, null));

            Assert.Equal(2, _messages.Count);
            Assert.Equal(project.NextSummaryEndOfDayTicks + TimeSpan.TicksPerDay, Repository.GetById(project.Id).NextSummaryEndOfDayTicks);
            Assert.Equal(project2.NextSummaryEndOfDayTicks + TimeSpan.TicksPerDay, Repository.GetById(project2.Id).NextSummaryEndOfDayTicks);
        }

        [Fact(Skip = "This fails because the global job lock doesn't run when there is a unit test.")]
        public void CanQueueNotificationsInMultiThreadedEnvironment() {
            _messages.Clear();
            const int numberOfProjects = 30;

            List<Project> projects = ProjectData.GenerateProjects(numberOfProjects, true, organizationId: TestConstants.OrganizationId3, nextSummaryEndOfDayTicks: DateTime.UtcNow.AddHours(OFFSET).AddMinutes(-1).Ticks).ToList();
            Repository.Add(projects);

            List<Project> projects2 = ProjectData.GenerateProjects(numberOfProjects, true, organizationId: TestConstants.OrganizationId4, nextSummaryEndOfDayTicks: DateTime.UtcNow.AddHours(OFFSET - 1).AddMinutes(-1).Ticks).ToList();
            Repository.Add(projects2);

            // TODO: We need to have some kind of lock to where we can unit test this.
            Parallel.For(0, 5, (i) => {
                var job = new DailyNotificationJob(Repository, _messageFactoryMock.Object);
                job.Run(new JobRunContext("Daily Summary", "", DateTime.Now, JobStatus.None, null, null, null));
            });

            Assert.Equal(numberOfProjects, _messages.Count);
            List<Project> updatedProjects = Repository.GetByOrganizationId(TestConstants.OrganizationId3).ToList();
            foreach (Project project in updatedProjects) {
                Project originalProject = projects.First(p => String.Equals(p.Id, project.Id));
                Assert.Equal(originalProject.NextSummaryEndOfDayTicks + TimeSpan.TicksPerDay, project.NextSummaryEndOfDayTicks);
            }

            List<Project> updatedProjects2 = Repository.GetByOrganizationId(TestConstants.OrganizationId4).ToList();
            foreach (Project project in updatedProjects2) {
                Project originalProject = projects.First(p => String.Equals(p.Id, project.Id));
                Assert.Equal(originalProject.NextSummaryEndOfDayTicks, project.NextSummaryEndOfDayTicks);
            }
        }

        protected override void SetUp() {
            _messageSubscriberMock.Setup(m => m.Publish(It.IsAny<SummaryNotification>())).Callback<SummaryNotification>(_messages.Add);
            _messageFactoryMock.Setup(m => m.CreateMessageProducer()).Returns(_messageSubscriberMock.Object);
        }

        protected override void TearDown() {
            _messages.Clear();
            base.TearDown();
        }
    }
}