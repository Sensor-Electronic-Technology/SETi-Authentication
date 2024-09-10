using FastEndpoints;
using Infrastructure.Services;
using SETiAuth.Domain.Shared.Constants;
using SETiAuth.Domain.Shared.Contracts.Requests;
using SETiAuth.Domain.Shared.Contracts.Responses;

namespace AuthApi.Endpoints;

public class GetUsersEndpoint:Endpoint<GetUsersRequest,GetUsersResponse> {
    private readonly AuthDataService _dataService;
    
    public GetUsersEndpoint(AuthDataService authDataService) {
        this._dataService = authDataService;
    }
    
    public override void Configure() {
        Post($"/api/{HttpClientConstants.GetUsersEndpoint}");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(GetUsersRequest req, CancellationToken ct) {
        if(string.IsNullOrEmpty(req.Role) || string.IsNullOrEmpty(req.AuthDomain)) {
            await SendAsync(new GetUsersResponse{Success = false,Message = "Role and AppDomain must have values",Users=[]},cancellation:ct);
            return;
        }
        var users=await this._dataService.GetUsers(req.AuthDomain,req.Role);
        await SendAsync(new GetUsersResponse{Success = true,Message = "Users retrieved successfully",Users=users},cancellation:ct);
    }
}