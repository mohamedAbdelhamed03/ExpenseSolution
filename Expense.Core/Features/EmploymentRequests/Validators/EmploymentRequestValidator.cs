using System;
// using FOE.HR.Data;
// using FOE.HR.DTOs;
// using FOE.HR.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using FluentValidation;

namespace FOE.HR.Features.EmploymentRequests.Validators
{
    public class EmploymentRequestValidator
    {
        // private readonly ApplicationDbContext _db;
        // private readonly IdentityDbContext _identity;
        private readonly IStringLocalizer<EmploymentRequestValidator> _localizer;

        public EmploymentRequestValidator(IStringLocalizer<EmploymentRequestValidator> localizer)
        {
            // _db = db;
            // _identity = identity;
            _localizer = localizer;
        }

        // TODO: Implement validation logic when DTOs are available
    }
}