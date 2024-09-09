using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.AccountManagement;
using Domain.Authentication;
using Domain.Settings;
using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using SETiAuth.Domain.Shared.Authentication;
using SETiAuth.Domain.Shared.Contracts.Requests;
using SETiAuth.Domain.Shared.Contracts.Responses;

namespace Infrastructure.Services;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class AuthService {
    private readonly AuthDataService _authDataService;
    private readonly SettingsService _settingsService;
    private readonly ILogger<AuthService> _logger;
    private LdapSettings _ldapSettings;
    
    public AuthService(ILogger<AuthService> logger,
        AuthDataService authDataService,
        SettingsService settingsService) {
        this._authDataService = authDataService;
        this._settingsService = settingsService;
        this._ldapSettings= new LdapSettings();
        this._ldapSettings.Password = "";
        this._ldapSettings.UserName = "";
        this._logger = logger;
    }
    
    public async Task<CreateAccountResponse> CreateAccount(CreateAccountRequest req) {
        if(string.IsNullOrEmpty(req.Username) || 
           string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Role) 
           || string.IsNullOrEmpty(req.AuthDomain)) {
            return new CreateAccountResponse() { Success = false, Message = "Invalid Request." };
        }

        if (!req.IsDomainUser && string.IsNullOrEmpty(req.Password)) {
            return new CreateAccountResponse() { Success = false, Message = "Local account must set the password"};
        }

        if (!req.IsDomainUser) {
            if (await this._authDataService.LocalUserExists(req.Username,req.AuthDomain)) {
                return new CreateAccountResponse() { Success = false, Message = "Username already exists" };
            }
        }
        var result=await this._authDataService.CreateAccount(req.Username,req.Email,req.Role,req.AuthDomain,req.IsDomainUser,req.Password);
        if (result.IsError) {
            return new CreateAccountResponse() { Success = false, Message = result.FirstError.Description };
        }
        return new CreateAccountResponse(){Success = true,Message = "Account created"};
    }
    
    public async Task<ErrorOr<UserSessionDto>> Login(string username, string password,bool isDomainUser, string authDomain) {
        if (isDomainUser) {
            if(await this.Auth(username,password)) {
                return await this._authDataService.LoginDomainAccount(username,authDomain);
            } else {
                return Error.Unauthorized(description:"Invalid username or password");
            }
        } else {
            return await this._authDataService.LoginLocalUserAccount(username,password,authDomain);
        }
    }
    
    private Task<bool> Auth(string username, string password) {
        try {
            using PrincipalContext context = new PrincipalContext(ContextType.Domain,
                this._ldapSettings.HostIp,
                this._ldapSettings.UserName,
                this._ldapSettings.Password);
            UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
            if (user != null) {
                if (context.ValidateCredentials(username, password)) {
                    this._logger.LogInformation("User authenticated. User: {User}", user.SamAccountName);
                    return Task.FromResult(true);
                }

                this._logger.LogWarning("Login Failed. User: {User}", user.SamAccountName);
                return Task.FromResult(true);
            }

            this._logger.LogWarning("Username not found: {Username}", username);
            return Task.FromResult(true);
        } catch (Exception e) {
            this._logger.LogError(e, "Error authenticating user");
            return Task.FromResult(false);
        }
    }
    
    /*public async Task Logout(ObjectId token) {
        await this._authDataService.Logout(token);
    }*/

    public async Task Load() {
        this._ldapSettings = await this._settingsService.GetLatestSettings();
    }
}