using System.Text;
using SETiAuth.Domain.Shared.Contracts;
using FastEndpoints;
using Infrastructure.Services;
using SETiAuth.Domain.Shared.Constants;
using SETiAuth.Domain.Shared.Contracts.Requests;
using SETiAuth.Domain.Shared.Contracts.Responses;

namespace AuthApi.Endpoints;

public class LoginEndpoint:Endpoint<LoginRequest,LoginResponse> {
    private readonly AuthService _authService;
    private readonly ILogger<LoginEndpoint> _logger;
    
    public LoginEndpoint(AuthService authService,ILogger<LoginEndpoint> logger) {
        _authService = authService;
        this._logger = logger;
    }
    
    public override void Configure() {
        Post($"/api/{HttpClientConstants.LoginEndpoint}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct) {
        if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Password) || string.IsNullOrEmpty(req.AuthDomain)) {
            await SendAsync(new LoginResponse() { Success = false, Message = "Invalid Request.  Error: Username and password are required" }, cancellation: ct);
            return;
        }
        var result = await _authService.Login(req.Username,req.Password,req.IsDomainUser,req.AuthDomain);
        if(!result.IsError) {
            await SendAsync(new LoginResponse() { UserSession = result.Value, Success = true }, cancellation: ct);
        } else {
            await SendAsync(new LoginResponse() {  Success = false, Message = result.FirstError.Description }, cancellation: ct);
        }
    }
}
