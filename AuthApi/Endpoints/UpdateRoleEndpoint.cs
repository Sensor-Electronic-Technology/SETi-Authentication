using System.Text;
using SETiAuth.Domain.Shared.Contracts;
using FastEndpoints;
using Infrastructure.Services;
using SETiAuth.Domain.Shared.Constants;
using SETiAuth.Domain.Shared.Contracts.Requests;
using SETiAuth.Domain.Shared.Contracts.Responses;

namespace AuthApi.Endpoints;

public class UpdateRoleEndpoint:Endpoint<UpdateRoleRequest,UpdateRoleResponse> {
    private readonly AuthDataService _dataService;
    private readonly ILogger<UpdateRoleEndpoint> _logger;
    
    public UpdateRoleEndpoint(AuthDataService dataService,ILogger<UpdateRoleEndpoint> logger) {
        _dataService = dataService;
        this._logger = logger;
    }
    
    public override void Configure() {
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
        var result = await this._dataService.UpdateUserRole(req.Username,req.AuthDomain,req.Role,req.IsDomainAccount);
        await SendAsync(new UpdateRoleResponse() {
            Success = !result.IsError,
            Message = !result.IsError ? "Role updated": result.FirstError.Description
        },cancellation:ct);
    }
}