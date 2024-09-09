namespace SETiAuth.Domain.Shared.Authentication;

public class UserSessionDto {
    public string Token { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
}