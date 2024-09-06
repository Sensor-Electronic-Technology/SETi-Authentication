using Domain.Shared.Constants;
using Domain.Shared.Contracts;
using Domain.Shared.Contracts.Requests;
using Domain.Shared.Contracts.Responses;
using FastEndpoints;
using Infrastructure.Services;

namespace AuthApi.Endpoints;

public class CreateAccountEndpoint:Endpoint<CreateAccountRequest,CreateAccountResponse> {
    private readonly AuthService _authService;
    
    public CreateAccountEndpoint(AuthService authService) {
        this._authService = authService;
    }
    
    public override void Configure() {
        Post($"/api/{HttpClientConstants.CreateAccountEndpoint}");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(CreateAccountRequest req, CancellationToken ct) {
        var result= await this._authService.CreateAccount(req);
        await SendAsync(result,cancellation:ct);
    }
}