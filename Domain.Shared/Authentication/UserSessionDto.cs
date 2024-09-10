namespace SETiAuth.Domain.Shared.Authentication;

public class UserSessionDto {
    public string Token { get; set; }
    public UserAccountDto UserAccount { get; set; }
}