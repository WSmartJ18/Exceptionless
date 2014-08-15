using System;
using Exceptionless.Core.Extensions;
using Exceptionless.Models;
using FluentValidation;

namespace Exceptionless.Core.Validation {
    public class MonthStackStatsValidator : AbstractValidator<MonthStackStats> {
        public MonthStackStatsValidator() {
            RuleFor(m => m.ProjectId).IsObjectId().WithMessage("Please specify a valid project id.");
            RuleFor(m => m.StackId).IsObjectId().WithMessage("Please specify a valid stack id.");
        }
    }
}