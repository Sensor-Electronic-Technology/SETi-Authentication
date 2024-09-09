using FastEndpoints;
using Infrastructure.Services;
using SETiAuth.Domain.Shared.Constants;
using SETiAuth.Domain.Shared.Contracts.Requests;
using SETiAuth.Domain.Shared.Contracts.Responses;

namespace AuthApi.Endpoints;

public class UpdateAuthDomainEndpoint:Endpoint<UpdateAuthDomainRequest,UpdateAuthDomainResponse> {
    private readonly AuthDataService _dataService;
    
    public UpdateAuthDomainEndpoint(AuthDataService dataService) {
        _dataService = dataService;
    }
    
    public override void Configure() {
        Post(HttpClientConstants.UpdateAuthDomainEndpoint);
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(UpdateAuthDomainRequest req, CancellationToken ct) {
        if(string.IsNullOrEmpty(req.Name) || req.Roles.Count == 0) {
            await SendAsync(
                new UpdateAuthDomainResponse() {
                    Success = false, Message = "Name, LdapOrganizationUnit, and Roles are required"
                }, cancellation: ct);
            return;
        }
        var success=await this._dataService.UpdateAuthDomain(req.Name,req.Description,req.Roles);
        await SendAsync(new UpdateAuthDomainResponse() {
            Success = success,
            Message = success ? "Domain created successfully" : "Error creating domain"
        }, cancellation: ct);
    }
}