namespace Domain.Shared.Contracts.Requests;

public class LoginRequest {
    public string Username { get; set; }
    public string Password { get; set; }
    public bool IsDomainUser { get; set; }
    public string? AuthDomain { get; set; }
}