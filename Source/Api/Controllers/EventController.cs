﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using Exceptionless.Api.Models;
using Exceptionless.Core.AppStats;
using Exceptionless.Core.Authorization;
using Exceptionless.Core.Extensions;
using Exceptionless.Core.Models;
using Exceptionless.Core.Plugins.Formatting;
using Exceptionless.Core.Queues;
using Exceptionless.Core.Queues.Models;
using Exceptionless.Core.Repositories;
using Exceptionless.Api.Utility;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using FluentValidation;

namespace Exceptionless.Api.Controllers {
    [RoutePrefix(API_PREFIX + "/events")]
    [Authorize(Roles = AuthorizationRoles.User)]
    public class EventController : RepositoryApiController<IEventRepository, PersistentEvent, PersistentEvent, PersistentEvent, UpdateEvent> {
        private readonly IProjectRepository _projectRepository;
        private readonly IStackRepository _stackRepository;
        private readonly IQueue<EventPost> _eventPostQueue;
        private readonly IQueue<EventUserDescription> _eventUserDescriptionQueue;
        private readonly IAppStatsClient _statsClient;
        private readonly IValidator<UserDescription> _userDescriptionValidator;
        private readonly FormattingPluginManager _formattingPluginManager;

        public EventController(IEventRepository repository, 
            IProjectRepository projectRepository, 
            IStackRepository stackRepository, 
            IQueue<EventPost> eventPostQueue, 
            IQueue<EventUserDescription> eventUserDescriptionQueue,
            IAppStatsClient statsClient,
            IValidator<UserDescription> userDescriptionValidator,
            FormattingPluginManager formattingPluginManager) : base(repository) {
            _projectRepository = projectRepository;
            _stackRepository = stackRepository;
            _eventPostQueue = eventPostQueue;
            _eventUserDescriptionQueue = eventUserDescriptionQueue;
            _statsClient = statsClient;
            _userDescriptionValidator = userDescriptionValidator;
            _formattingPluginManager = formattingPluginManager;
        }

        [HttpGet]
        [Route]
        public IHttpActionResult Get(string before = null, string after = null, int limit = 10, string mode = null) {
            var options = new PagingOptions { Before = before, After = after, Limit = limit };
            var results = _repository.GetByOrganizationIds(GetAssociatedOrganizationIds(), options);

            if (!String.IsNullOrEmpty(mode) && String.Equals(mode, "summary", StringComparison.InvariantCultureIgnoreCase))
                return OkWithResourceLinks(results.Select(e => new EventSummaryModel(e.Id, e.Date, _formattingPluginManager.GetEventSummaryData(e))).ToList(), options.HasMore, e => String.Concat(e.Date.UtcTicks.ToString(), "-", e.Id), isDescending: true);

            return OkWithResourceLinks(results, options.HasMore, e => String.Concat(e.Date.UtcTicks.ToString(), "-", e.Id), isDescending: true);
        }

        [HttpGet]
        [Route("~/" + API_PREFIX + "/projects/{projectId:objectid}/events")]
        public IHttpActionResult GetByProjectId(string projectId, string before = null, string after = null, int limit = 10, string mode = null) {
            if (String.IsNullOrEmpty(projectId))
                return NotFound();

            var project = _projectRepository.GetById(projectId, true);
            if (project == null || !CanAccessOrganization(project.OrganizationId))
                return NotFound();

            var options = new PagingOptions { Before = before, After = after, Limit = limit };
            var results = _repository.GetByProjectId(projectId, options);

            if (!String.IsNullOrEmpty(mode) && String.Equals(mode, "summary", StringComparison.InvariantCultureIgnoreCase))
                return OkWithResourceLinks(results.Select(e => new EventSummaryModel(e.Id, e.Date, _formattingPluginManager.GetEventSummaryData(e))).ToList(), options.HasMore, e => String.Concat(e.Date.UtcTicks.ToString(), "-", e.Id), isDescending: true);

            return OkWithResourceLinks(results, options.HasMore, e => String.Concat(e.Date.UtcTicks.ToString(), "-", e.Id), isDescending: true);
        }

        [HttpGet]
        [Route("~/" + API_PREFIX + "/stacks/{stackId:objectid}/events")]
        public IHttpActionResult GetByStackId(string stackId, string before = null, string after = null, int limit = 10, string mode = null) {
            if (String.IsNullOrEmpty(stackId))
                return NotFound();

            var stack = _stackRepository.GetById(stackId, true);
            if (stack == null || !CanAccessOrganization(stack.OrganizationId))
                return NotFound();

            var options = new PagingOptions { Before = before, After = after, Limit = limit };
            var results = _repository.GetByStackId(stackId, options);

            if (!String.IsNullOrEmpty(mode) && String.Equals(mode, "summary", StringComparison.InvariantCultureIgnoreCase))
                return OkWithResourceLinks(results.Select(e => new EventSummaryModel(e.Id, e.Date, _formattingPluginManager.GetEventSummaryData(e))).ToList(), options.HasMore, e => String.Concat(e.Date.UtcTicks.ToString(), "-", e.Id), isDescending: true);

            return OkWithResourceLinks(results, options.HasMore, e => String.Concat(e.Date.UtcTicks.ToString(), "-", e.Id), isDescending: true);
        }

        [HttpGet]
        [Route("{id:objectid}")]
        public override IHttpActionResult GetById(string id) {
            return base.GetById(id);
        }
        
        [HttpGet]
        [Route("by-ref/{referenceId:minlength(8)}")]
        [Route("~/api/v2/projects/{projectId:objectid}/events/by-ref/{referenceId:minlength(8)}")]
        public IHttpActionResult GetByReferenceId(string referenceId, string projectId = null) {
            if (String.IsNullOrEmpty(referenceId))
                return NotFound();

            if (projectId == null)
                projectId = User.GetDefaultProjectId();

            // must have a project id
            if (String.IsNullOrEmpty(projectId))
                return BadRequest("No project id specified and no default project was found.");

            var project = _projectRepository.GetById(projectId, true);
            if (project == null || !User.GetOrganizationIds().ToList().Contains(project.OrganizationId))
                return NotFound();

            var results = _repository.GetByReferenceId(projectId, referenceId);
            return Ok(results);
        }

        [HttpPost]
        [Route("by-ref/{referenceId:minlength(8)}/user-description")]
        [Route("~/api/v2/projects/{projectId:objectid}/events/by-ref/{referenceId:minlength(8)}/user-description")]
        [OverrideAuthorization]
        [Authorize(Roles = AuthorizationRoles.UserOrClient)]
        public async Task<IHttpActionResult> SetUserDescription(string referenceId, UserDescription description, string projectId = null) {
            _statsClient.Counter(StatNames.EventsUserDescriptionSubmitted);
            
            if (String.IsNullOrEmpty(referenceId))
                return NotFound();

            if (description == null)
                return BadRequest("Description must be specified.");

            var result = _userDescriptionValidator.Validate(description);
            if (!result.IsValid)
                return BadRequest(result.Errors.ToErrorMessage());

            if (projectId == null)
                projectId = User.GetDefaultProjectId();

            // must have a project id
            if (String.IsNullOrEmpty(projectId))
                return BadRequest("No project id specified and no default project was found.");

            var project = _projectRepository.GetById(projectId, true);
            if (project == null || !User.GetOrganizationIds().ToList().Contains(project.OrganizationId))
                return NotFound();

            var eventUserDescription = Mapper.Map<UserDescription, EventUserDescription>(description);
            eventUserDescription.ProjectId = projectId;
            eventUserDescription.ReferenceId = referenceId;

            await _eventUserDescriptionQueue.EnqueueAsync(eventUserDescription);
            _statsClient.Counter(StatNames.EventsUserDescriptionQueued);

            return StatusCode(HttpStatusCode.Accepted);
        }

        [HttpPatch]
        [Route("~/api/v1/error/{id:objectid}")]
        [OverrideAuthorization]
        [Authorize(Roles = AuthorizationRoles.UserOrClient)]
        [ConfigurationResponseFilter]
        public async Task<IHttpActionResult> LegacyPatch(string id, Delta<UpdateEvent> changes) {
            if (changes == null)
                return Ok();

            if (changes.UnknownProperties.ContainsKey("UserEmail"))
                changes.TrySetPropertyValue("EmailAddress", changes.UnknownProperties["UserEmail"]);
            if (changes.UnknownProperties.ContainsKey("UserDescription"))
                changes.TrySetPropertyValue("Description", changes.UnknownProperties["UserDescription"]);

            var userDescription = new UserDescription();
            changes.Patch(userDescription);

            return await SetUserDescription(id, userDescription);
        }

        [HttpPost]
        [Route("~/api/v{version:int=1}/error")]
        [Route("~/api/v{version:int=1}/events")]
        [Route("~/api/v{version:int=1}/projects/{projectId:objectid}/events")]
        [OverrideAuthorization]
        [Authorize(Roles = AuthorizationRoles.UserOrClient)]
        [ConfigurationResponseFilter]
        public async Task<IHttpActionResult> Post([NakedBody]byte[] data, string projectId = null, int version = 1, [UserAgent]string userAgent = null) {
            _statsClient.Counter(StatNames.PostsSubmitted);
            if (projectId == null)
                projectId = User.GetDefaultProjectId();

            // must have a project id
            if (String.IsNullOrEmpty(projectId))
                return BadRequest("No project id specified and no default project was found.");

            var project = _projectRepository.GetById(projectId, true);
            if (project == null || !User.GetOrganizationIds().ToList().Contains(project.OrganizationId))
                return NotFound();

            string contentEncoding = Request.Content.Headers.ContentEncoding.ToString();
            bool isCompressed = contentEncoding == "gzip" || contentEncoding == "deflate";
            if (!isCompressed && data.Length > 1000) {
                data = data.Compress();
                contentEncoding = "gzip";
            }

            await _eventPostQueue.EnqueueAsync(new EventPost {
                MediaType = Request.Content.Headers.ContentType.MediaType,
                CharSet = Request.Content.Headers.ContentType.CharSet,
                ProjectId = projectId,
                UserAgent = userAgent,
                ApiVersion = version,
                Data = data,
                ContentEncoding = contentEncoding
            });
            _statsClient.Counter(StatNames.PostsQueued);

            return StatusCode(HttpStatusCode.Accepted);
        }

        protected override void CreateMaps() {
            if (Mapper.FindTypeMapFor<UserDescription, EventUserDescription>() == null)
                Mapper.CreateMap<UserDescription, EventUserDescription>();

            base.CreateMaps();
        }
    }
}