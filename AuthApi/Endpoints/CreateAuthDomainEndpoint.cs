using FastEndpoints;
using Infrastructure.Services;
using SETiAuth.Domain.Shared.Constants;
using SETiAuth.Domain.Shared.Contracts.Requests;
using SETiAuth.Domain.Shared.Contracts.Responses;

namespace AuthApi.Endpoints;

public class CreateAuthDomainEndpoint:Endpoint<CreateAuthDomainRequest,CreateAuthDomainResponse> {
    private readonly AuthDataService _dataService;
    
    public CreateAuthDomainEndpoint(AuthDataService dataService) {
        _dataService = dataService;
    }
    
    public override void Configure() {
        Post(HttpClientConstants.CreateAuthDomainEndpoint);
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(CreateAuthDomainRequest req, CancellationToken ct) {
        if(string.IsNullOrEmpty(req.Name) || req.Roles.Count == 0) {
            await SendAsync(
                new CreateAuthDomainResponse() {
                    Success = false, Message = "Name, LdapOrganizationUnit, and Roles are required"
                }, cancellation: ct);
            return;
        }
        var result=await this._dataService.CreateAuthDomain(req.Name,req.Description,req.Roles);
        await SendAsync(new CreateAuthDomainResponse() {
            Success = !result.IsError,
            Message = !result.IsError ? "Domain created successfully" : result.FirstError.Description
        }, cancellation: ct);
    }
}