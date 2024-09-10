namespace SETiAuth.Domain.Shared.Contracts.Requests;

public class GetUsersRequest {
    public string? AuthDomain { get; set; }
    public string? Role { get; set; }
}