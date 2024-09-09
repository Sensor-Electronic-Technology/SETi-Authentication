namespace SETiAuth.Domain.Shared.Contracts.Requests;

public class CreateAccountRequest {
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsDomainUser { get; set; }
    public string? AuthDomain { get; set; }
}