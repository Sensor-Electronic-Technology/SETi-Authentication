using SETiAuth.Domain.Shared.Authentication;

namespace SETiAuth.Domain.Shared.Contracts.Responses;

public class GetUsersResponse {
    public List<UserAccountDto>? Users { get; set; }
    public string Message { get; set; } = null!;
    public bool Success { get; set; }
}