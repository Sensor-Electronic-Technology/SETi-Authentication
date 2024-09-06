using Domain.Shared.Authentication;

namespace Domain.Shared.Contracts.Responses;

public class LoginResponse {
    public UserSessionDto? UserSession { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}