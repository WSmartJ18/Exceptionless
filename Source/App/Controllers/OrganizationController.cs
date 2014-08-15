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
using Exceptionless.App.Models.Organization;
using Exceptionless.Core.Repositories;
using Exceptionless.Models;
using Stripe;

namespace Exceptionless.App.Controllers {
    [Authorize]
    public class OrganizationController : ExceptionlessController {
        private readonly IOrganizationRepository _repository;

        public OrganizationController(IOrganizationRepository repository) {
            _repository = repository;
        }

        [HttpGet]
        public ActionResult List() {
            return View();
        }

        [HttpGet]
        public ActionResult Manage(string id) {
            if (String.IsNullOrEmpty(id) || !CanAccessOrganization(id))
                throw new ArgumentException("Invalid organization id.", "id"); // TODO: These should probably throw http Response exceptions.

            Organization organization = _repository.GetById(id);
            if (String.IsNullOrEmpty(id) || organization == null)
                return RedirectToAction("List");

            return View(organization);
        }


        [HttpGet]
        public ActionResult Payment(string id) {
            if (String.IsNullOrEmpty(id))
                return HttpNotFound();

            if (!id.StartsWith("in_"))
                id = "in_" + id;

            var invoiceService = new StripeInvoiceService();
            StripeInvoice invoice = invoiceService.Get(id);
            if (invoice == null)
                return HttpNotFound();

            Organization org = _repository.GetByStripeCustomerId(invoice.CustomerId);
            if (org == null)
                return HttpNotFound();

            if (!CanAccessOrganization(org.Id))
                return HttpNotFound();

            return View(new InvoiceModel { Invoice = invoice, Organization = org });
        }

        [HttpGet]
        public ActionResult Suspended(string id = null) {
            if (String.IsNullOrEmpty(id) || !CanAccessOrganization(id))
                return View();

            Organization organization = _repository.GetById(id);
            if (organization.IsSuspended)
                return View(organization);

            return RedirectToAction("Index", "Project");
        }
    }
}