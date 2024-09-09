namespace SETiAuth.Domain.Shared.Contracts.Requests;

public class UpdateRoleRequest {
    public string? Username { get; set; }
    public string? Role { get; set; }
    public string? AuthDomain { get; set; }
    public bool IsDomainAccount { get; set; }
}