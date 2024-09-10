namespace SETiAuth.Domain.Shared.Contracts.Requests;

public class GetUsersRequest {
    public string? AppDomain { get; set; }
    public string? Role { get; set; }
}