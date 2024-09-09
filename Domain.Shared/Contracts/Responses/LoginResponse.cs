using SETiAuth.Domain.Shared.Authentication;

namespace SETiAuth.Domain.Shared.Contracts.Responses;

public class LoginResponse {
    public UserSessionDto? UserSession { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}