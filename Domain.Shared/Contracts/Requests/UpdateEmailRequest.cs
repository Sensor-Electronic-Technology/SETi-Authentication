namespace SETiAuth.Domain.Shared.Contracts.Requests;

public class UpdateEmailRequest {
    public string Username { get; set; }
    public string Email { get; set; }
    public bool IsDomainAccount { get; set; }
    public string AuthDomain { get; set; }
}