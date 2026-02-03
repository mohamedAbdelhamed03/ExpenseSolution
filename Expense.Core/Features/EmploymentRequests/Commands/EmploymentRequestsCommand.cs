using System;
using MediatR;
// using FOE.HR.DTOs;
// using FOE.HR.Services;
// using FOE.HR.Helper;

namespace FOE.HR.Features.EmploymentRequests.Commands
{
    // TODO: Implement commands when DTOs are available
    public class CreateEmploymentRequestCommand : IRequest<bool>
    {
        // public EmploymentRequestCreateDto Request { get; set; }
        // public RequestContext Context { get; set; }
    }

    public class UpdateEmploymentRequestCommand : IRequest<bool>
    {
        // public EmploymentRequestUpdateDto Request { get; set; }
        // public RequestContext Context { get; set; }
    }

    public class PatchEmploymentRequestCommand : IRequest<bool>
    {
        // public EmploymentRequestPatchDto Request { get; set; }
        // public RequestContext Context { get; set; }
    }

    public class GetEmploymentRequestByIdQuery : IRequest<bool>
    {
        // public EmploymentRequestGetDto Request { get; set; }
    }

    public class GetAllEmploymentRequestsQuery : IRequest<bool>
    {
        // public EmploymentRequestGetDto Request { get; set; }
    }
}