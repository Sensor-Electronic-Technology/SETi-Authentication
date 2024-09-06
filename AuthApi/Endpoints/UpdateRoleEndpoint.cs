using System.Text;
using Domain.Shared.Constants;
using Domain.Shared.Contracts;
using Domain.Shared.Contracts.Requests;
using Domain.Shared.Contracts.Responses;
using FastEndpoints;
using Infrastructure.Services;

namespace AuthApi.Endpoints;

public class UpdateRoleEndpoint:Endpoint<UpdateRoleRequest,UpdateRoleResponse> {
    private readonly AuthDataService _authDataService;
    private readonly ILogger<UpdateRoleEndpoint> _logger;
    
    public UpdateRoleEndpoint(AuthDataService authDataService,ILogger<UpdateRoleEndpoint> logger) {
        _authDataService = authDataService;
        this._logger = logger;
    }
    
    public override void Configure() {
        //Put("/api/email/update");
        Put($"/api/{HttpClientConstants.UpdateRoleEndpoint}");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(UpdateRoleRequest req, CancellationToken ct) {
        if(string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Role) || string.IsNullOrEmpty(req.AuthDomain)) {
            await SendAsync(new UpdateRoleResponse() {
                Success = false,
                Message = "Invalid Request"
            },cancellation:ct);
            return;
        }
        var result = await _authDataService.UpdateUserRole(req.Username,req.AuthDomain,req.Role,req.IsDomainAccount);
        await SendAsync(new UpdateRoleResponse() {
            Success = !result.IsError,
            Message = !result.IsError ? "Role updated": result.FirstError.Description
        },cancellation:ct);
    }
}