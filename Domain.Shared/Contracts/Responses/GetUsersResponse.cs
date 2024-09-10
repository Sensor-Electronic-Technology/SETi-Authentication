using SETiAuth.Domain.Shared.Authentication;

namespace SETiAuth.Domain.Shared.Contracts.Responses;

public class GetUsersResponse {
    public List<UserAccountDto>? Users { get; set; }
}