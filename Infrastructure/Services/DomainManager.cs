using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Text.RegularExpressions;
using Domain.Authentication;
using Domain.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ErrorOr;

namespace Infrastructure.Services;

public class CreateGroupInput {
    public string SamName { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
}

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class DomainManager {
    public static string ProgramAccessDn => "OU={orgUnit},OU=Program Access,OU=Groups,OU=SETI,DC=seti,DC=com";
    public static readonly string SetiUserContainer = "OU=SETI Users,DC=seti,DC=com";
    private readonly IConfiguration _config;
    private readonly SettingsService _settingsService;
    private readonly ILogger<DomainManager> _logger;
    private LdapSettings _ldapSettings;

    public DomainManager(ILogger<DomainManager> logger,
        SettingsService settingsService,
        IConfiguration configuration) {
        this._settingsService = settingsService;
        this._ldapSettings = new LdapSettings { Password = "", UserName = "" };
        this._logger = logger;
        this._config = configuration;
    }

    public Task<bool> Auth(string username, string password) {
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

    public Task<ErrorOr<AuthDomain>> CreateNewAuthDomain(string authDomain, string description,
        List<CreateGroupInput> groups) {
        try {
            using DirectoryEntry ldapConnection = new DirectoryEntry(
                this._ldapSettings.DirectoryLdapPath,
                this._ldapSettings.UserName,
                this._ldapSettings.Password);
            ldapConnection.RefreshCache();
            DirectorySearcher source = new DirectorySearcher(ldapConnection);
            source.Filter = $"OU={authDomain}";
            source.SearchScope = SearchScope.Subtree;
            var searchResult = source.FindOne();
            if (searchResult == null) {
                AuthDomain newAuthDomain = new AuthDomain() {
                    _id = authDomain, Description = description, Roles = groups.Select(e => e.DisplayName).ToList()
                };
                DirectoryEntry newOrgUnit = ldapConnection.Children.Add($"OU={authDomain}", "organizationalUnit");
                newOrgUnit.Properties["description"].Value = description;
                newOrgUnit.CommitChanges();
                foreach (var group in groups) {
                    var groupObj = newOrgUnit.Children.Add($"CN={group.SamName}", "group");
                    groupObj.Properties["sAMAccountName"].Value = group.SamName;
                    groupObj.Properties["displayName"].Value = group.DisplayName;
                    groupObj.Properties["description"].Value = group.Description;
                    groupObj.CommitChanges();
                    this._logger.LogInformation("Created AuthDomain. OU:{OrgUnit} Group: {Group}", authDomain,
                        group.SamName);
                }

                ErrorOr<AuthDomain> result = newAuthDomain;
                return Task.FromResult(result);
            } else {
                this._logger.LogWarning("OrgUnit already exists, skipping creation.  OrgUnit: {OrgUnit}", authDomain);
                ErrorOr<AuthDomain> result = Error.Conflict(description: $"OrgUnit {authDomain} already exists");
                /*return Task.FromResult(ErrorOr<AuthDomain>.From([Error.Conflict()]));*/
                return Task.FromResult(result);
            }
        } catch (Exception e) {
            this._logger.LogError(e, "Exception thrown while creating new AuthDomain: {AuthDomain}", authDomain);
            ErrorOr<AuthDomain> result = Error.Failure(description: $"Exception thrown while creating " +
                                                                    $"new AuthDomain: {authDomain} \n {e.Message}");
            return Task.FromResult(result);
        }
    }

    public Task<ErrorOr<string>> UpdateAuthDomain(string authDomain, CreateGroupInput inputGroup) {
        try {
            using DirectoryEntry ldapConnection = new DirectoryEntry(
                this._ldapSettings.DirectoryLdapPath,
                this._ldapSettings.UserName,
                this._ldapSettings.Password);
            ldapConnection.RefreshCache();
            DirectorySearcher source = new DirectorySearcher(ldapConnection);
            source.Filter = $"OU={authDomain}";
            source.SearchScope = SearchScope.Subtree;
            var searchResult = source.FindOne();
            if (searchResult != null) {
                DirectorySearcher groupSource = new DirectorySearcher(ldapConnection);
                source.Filter = $"CN={inputGroup.SamName}";
                source.SearchScope = SearchScope.Subtree;
                var groupSearchResult = groupSource.FindOne();
                if (groupSearchResult == null) {
                    var groupObj = searchResult.GetDirectoryEntry().Children.Add($"CN={inputGroup.SamName}", "group");
                    groupObj.Properties["sAMAccountName"].Value = inputGroup.SamName;
                    groupObj.Properties["displayName"].Value = inputGroup.DisplayName;
                    groupObj.Properties["description"].Value = inputGroup.Description;
                    groupObj.CommitChanges();
                    this._logger.LogInformation("Created AuthDomain. OU:{OrgUnit} Group: {Group}", authDomain,
                        inputGroup.SamName);
                    ErrorOr<string> result = inputGroup.DisplayName;
                    return Task.FromResult(result);
                } else {
                    this._logger.LogWarning("Group already exists.  Group: {Group}", inputGroup.SamName);
                    ErrorOr<string> result = Error.NotFound(description: $"Group {inputGroup.SamName} already exists");
                    return Task.FromResult(result);
                }
            } else {
                this._logger.LogWarning("OrgUnit not found.  OrgUnit: {OrgUnit}", authDomain);
                ErrorOr<string> result = Error.Conflict(description: $"OrgUnit {authDomain} not found");
                return Task.FromResult(result);
            }
        } catch (Exception e) {
            this._logger.LogError(e, "Exception thrown while updating {AuthDomain}", authDomain);
            ErrorOr<string> result = Error.Failure(description: $"Exception thrown while creating " +
                                                                $"new AuthDomain: {authDomain} \n {e.Message}");
            return Task.FromResult(result);
        }
    }

    public Task<bool> AddUserToGroup(string user, string groupName) {
        try {
            using PrincipalContext pc = new PrincipalContext(ContextType.Domain,
                this._ldapSettings.HostIp,
                this._ldapSettings.UserName,
                this._ldapSettings.Password);
            GroupPrincipal group = GroupPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, groupName);
            group.Members.Add(pc, IdentityType.SamAccountName, user);
            group.Save();
            return Task.FromResult(true);
        } catch {
            return Task.FromResult(false);
        }
    }

    public Task<List<string>> GetUserAuthDomainGroup(string username, string authDomain) {
        using PrincipalContext pc = new PrincipalContext(ContextType.Domain,
            this._ldapSettings.HostIp,
            this._ldapSettings.UserName,
            this._ldapSettings.Password);
        UserPrincipal userprinciple = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, username);
        using PrincipalContext context = new PrincipalContext(ContextType.Domain,
            this._ldapSettings.HostIp,
            DomainManager.ProgramAccessDn.Replace("{orgUnit}", authDomain),
            this._ldapSettings.UserName,
            this._ldapSettings.Password);
        GroupPrincipal findAllGroups = new GroupPrincipal(context, "*");
        PrincipalSearcher ps = new PrincipalSearcher(findAllGroups);
        List<string> groups = [];
        foreach (var group in ps.FindAll()) {
            if (userprinciple.IsMemberOf((GroupPrincipal)group)) {
                groups.Add(((GroupPrincipal)group).SamAccountName);
            }
        }
        return Task.FromResult(groups);
    }

    public bool RemoveUserFromGroup(string user, string groupName) {
        try {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain,
                       this._ldapSettings.HostIp,
                       this._ldapSettings.UserName,
                       this._ldapSettings.Password)) {
                UserPrincipal up = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, user);
                GroupPrincipal group = GroupPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, groupName);
                if (group.Members.Remove(pc, IdentityType.SamAccountName, user)) {
                    group.Save();
                    return true;
                } else {
                    return false;
                }
            }
        } catch {
            return false;
        }
    }

    public IList<string> GetGroupMembers(string groupSamName) {
        try {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain,
                       this._ldapSettings.HostIp,
                       this._ldapSettings.UserName,
                       this._ldapSettings.Password)) {
                UserPrincipal up = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, groupSamName);
                if (up != null) {
                    List<string> grpList = new List<string>();
                    up.GetGroups(pc).ToList().ForEach(grp => { grpList.Add(grp.SamAccountName); });
                    return grpList;
                } else {
                    return [];
                }
            }
        } catch {
            return [];
        }
    }

    public IList<UserPrincipal> GetSetiUsers() {
        try {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, this._ldapSettings.HostIp,
                       DomainManager.SetiUserContainer,
                       this._ldapSettings.UserName,
                       this._ldapSettings.Password)) {
                UserPrincipal allUsers = new UserPrincipal(pc);
                PrincipalSearcher ps = new PrincipalSearcher(allUsers);
                List<UserPrincipal> users = [];
                foreach (var principal in ps.FindAll()) {
                    var user = (UserPrincipal)principal;
                    users.Add(user);
                }

                return users;
            }
        } catch {
            return [];
        }
    }

    public DomainUserAccount? GetDomainUser(string userName) {
        try {
            using PrincipalContext pc = new PrincipalContext(ContextType.Domain,
                this._ldapSettings.HostIp,
                this._ldapSettings.UserName,
                this._ldapSettings.Password);
            UserPrincipal userPrinciple = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, userName);
            var user = new DomainUserAccount() {
                FirstName = userPrinciple.GivenName,
                LastName = userPrinciple.Surname,
                Email = userPrinciple.EmailAddress,
                _id = userPrinciple.SamAccountName
            };
            return user;
        } catch {
            return null;
        }
    }

    public Task<bool> DomainUserExists(string username) {
        using PrincipalContext context = new PrincipalContext(ContextType.Domain,
            this._ldapSettings.HostIp,
            this._ldapSettings.UserName,
            this._ldapSettings.Password);
        UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
        return Task.FromResult(user != null);
    }

    public Task<ErrorOr<Success>> RemoveUserFromAll(string authDomain, string username) {
        try {
            using PrincipalContext pc = new PrincipalContext(ContextType.Domain,
                this._ldapSettings.HostIp,
                this._ldapSettings.UserName,
                this._ldapSettings.Password);
            UserPrincipal userprinciple = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, username);
            using PrincipalContext context = new PrincipalContext(ContextType.Domain,
                this._ldapSettings.HostIp,
                DomainManager.ProgramAccessDn.Replace("{orgUnit}", authDomain),
                this._ldapSettings.UserName,
                this._ldapSettings.Password);
            GroupPrincipal findAllGroups = new GroupPrincipal(context, "*");
            PrincipalSearcher ps = new PrincipalSearcher(findAllGroups);
            foreach (var group in ps.FindAll()) {
                if (userprinciple.IsMemberOf((GroupPrincipal)group)) {
                    ((GroupPrincipal)group).Members.Remove(pc, IdentityType.SamAccountName,
                        userprinciple.SamAccountName);
                    ((GroupPrincipal)group).Save();
                }
            }
            ErrorOr<Success> result = Result.Success;
            return Task.FromResult(result);
        } catch (Exception e) {
            ErrorOr<Success> result =
                Error.Failure(description: $"Exception thrown while removing user from all groups: {e.Message}");
            return Task.FromResult(result);
        }
    }


    public async Task Load() {
        this._ldapSettings = await this._settingsService.GetLatestSettings();
    }
}