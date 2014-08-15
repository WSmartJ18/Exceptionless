using System;
using Exceptionless.Core.Extensions;
using Exceptionless.Models.Admin;
using FluentValidation;

namespace Exceptionless.Core.Validation {
    public class ApplicationValidator : AbstractValidator<Application> {
        public ApplicationValidator() {
            RuleFor(a => a.OrganizationId).IsObjectId().WithMessage("Please specify a valid organization id.");
            RuleFor(a => a.Name).NotEmpty().WithMessage("Please specify a valid name.");

            // TODO: Add additional rules.
        }
    }
}