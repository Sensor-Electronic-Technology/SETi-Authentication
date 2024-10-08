﻿using SETiAuth.Domain.Shared.Contracts;
using FastEndpoints;
using Infrastructure.Services;
using SETiAuth.Domain.Shared.Constants;
using SETiAuth.Domain.Shared.Contracts.Requests;
using SETiAuth.Domain.Shared.Contracts.Responses;
namespace AuthApi.Endpoints;

public class UpdateEmailEndpoint:Endpoint<UpdateEmailRequest,UpdateEmailResponse> {
    private readonly AuthDataService _authDataService;
    private readonly ILogger<UpdateEmailEndpoint> _logger;
    
    public UpdateEmailEndpoint(AuthDataService authDataService,ILogger<UpdateEmailEndpoint> logger) {
        _authDataService = authDataService;
        this._logger = logger;
    }
    public override void Configure() {
        Put($"/api/{HttpClientConstants.UpdateEmailEndpoint}");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(UpdateEmailRequest req, CancellationToken ct) {
        var result= await _authDataService.UpdateUserEmail(req.Username,req.Email,req.IsDomainAccount,req.AuthDomain);
        await SendAsync(new UpdateEmailResponse(){Success = result},cancellation:ct);
    }
}