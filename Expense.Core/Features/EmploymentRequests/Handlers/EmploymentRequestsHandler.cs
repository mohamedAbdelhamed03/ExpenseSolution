using System;
using Expense.Core.Features.EmploymentRequests.Commands;
using MediatR;

namespace Expense.Core.Features.EmploymentRequests.Handlers
{
    // TODO: Implement handlers when services and DTOs are available
    public class CreateEmploymentRequestHandler : IRequestHandler<CreateEmploymentRequestCommand, bool>
    {
        // private readonly IEmploymentRequestService _employmentRequestService;

        // public CreateEmploymentRequestHandler(IEmploymentRequestService employmentRequestService)
        // {
        //     _employmentRequestService = employmentRequestService;
        // }

        public async Task<bool> Handle(CreateEmploymentRequestCommand request, CancellationToken cancellationToken)
        {
            // return await _employmentRequestService.CreateAsync(request.Request, request.Context);
            return await Task.FromResult(true);
        }
    }

    public class UpdateEmploymentRequestHandler : IRequestHandler<UpdateEmploymentRequestCommand, bool>
    {
        // private readonly IEmploymentRequestService _employmentRequestService;

        // public UpdateEmploymentRequestHandler(IEmploymentRequestService employmentRequestService)
        // {
        //     _employmentRequestService = employmentRequestService;
        // }

        public async Task<bool> Handle(UpdateEmploymentRequestCommand request, CancellationToken cancellationToken)
        {
            // return await _employmentRequestService.UpdateAsync(request.Request, request.Context);
            return await Task.FromResult(true);
        }
    }

    public class PatchEmploymentRequestHandler : IRequestHandler<PatchEmploymentRequestCommand, bool>
    {
        // private readonly IEmploymentRequestService _employmentRequestService;

        // public PatchEmploymentRequestHandler(IEmploymentRequestService employmentRequestService)
        // {
        //     _employmentRequestService = employmentRequestService;
        // }

        public async Task<bool> Handle(PatchEmploymentRequestCommand request, CancellationToken cancellationToken)
        {
            // return await _employmentRequestService.PatchAsync(request.Request, request.Context);
            return await Task.FromResult(true);
        }
    }

    public class GetEmploymentRequestByIdHandler : IRequestHandler<GetEmploymentRequestByIdQuery, bool>
    {
        // private readonly IEmploymentRequestService _employmentRequestService;

        // public GetEmploymentRequestByIdHandler(IEmploymentRequestService employmentRequestService)
        // {
        //     _employmentRequestService = employmentRequestService;
        // }

        public async Task<bool> Handle(GetEmploymentRequestByIdQuery request, CancellationToken cancellationToken)
        {
            // return await _employmentRequestService.GetByIdAsync(request.Request);
            return await Task.FromResult(true);
        }
    }

    public class GetAllEmploymentRequestsHandler : IRequestHandler<GetAllEmploymentRequestsQuery, bool>
    {
        // private readonly IEmploymentRequestService _employmentRequestService;

        // public GetAllEmploymentRequestsHandler(IEmploymentRequestService employmentRequestService)
        // {
        //     _employmentRequestService = employmentRequestService;
        // }

        public async Task<bool> Handle(GetAllEmploymentRequestsQuery request, CancellationToken cancellationToken)
        {
            // return await _employmentRequestService.GetAllAsync(request.Request);
            return await Task.FromResult(true);
        }
    }
}