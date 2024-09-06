using Domain.Shared.Constants;
using Domain.Shared.Contracts;
using Domain.Shared.Contracts.Requests;
using FastEndpoints;
using Infrastructure.Services;
using MongoDB.Bson;

namespace AuthApi.Endpoints;

public class LogoutEndpoint:Endpoint<LogoutRequest,EmptyResponse> {
    private readonly AuthService _authService;
    private readonly ILogger<LogoutEndpoint> _logger;
    
    public LogoutEndpoint(AuthService authService,ILogger<LogoutEndpoint> logger) {
        _authService = authService;
        this._logger = logger;
    }
    
    public override void Configure() {
        //Post("/api/logout");
        Post($"/api/{HttpClientConstants.LogoutEndpoint}");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(LogoutRequest req, CancellationToken ct) {
        if(ObjectId.TryParse(req.Token, out var token)) {
            await _authService.Logout(token);
            await SendAsync(new EmptyResponse(), cancellation: ct);
        } else {
            await SendAsync(new EmptyResponse(), cancellation: ct);
        }
    }
    
}