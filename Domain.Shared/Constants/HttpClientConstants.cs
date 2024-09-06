namespace Domain.Shared.Constants;

public static class HttpClientConstants {
    public static string LoginApiUrl = "http://localhost:5243/api/";
    //public static string LoginApiUrl = "http://172.20.4.20:5000/api/";
    
    public static string LoginEndpoint => "login";
    public static string LogoutEndpoint=> "logout";
    public static string UpdateEmailEndpoint=> "account/email/update";
    public static string UpdateRoleEndpoint => "account/role/update";
    public static string CreateAccountEndpoint => "account/create";
}