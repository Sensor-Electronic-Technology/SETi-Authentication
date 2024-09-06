using Domain.Authentication;
using Domain.Settings;
using Domain.Shared.Authentication;
using Effortless.Net.Encryption;
using ErrorOr;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Services;

public class AuthDataService {
    private readonly IMongoCollection<UserSession> _userSessionCollection;
    private readonly IMongoCollection<DomainUserAccount> _domainAccountCollection;
    private readonly IMongoCollection<LocalUserAccount> _localUserAccountCollection;
    private readonly IMongoCollection<AuthDomain> _domainCollection;
    
    public AuthDataService(IMongoClient client,IOptions<DatabaseSettings> settings) {
        IMongoDatabase database = client.GetDatabase(settings.Value.AuthenticationDatabase ?? "auth_db");
        this._userSessionCollection = database.GetCollection<UserSession>(settings.Value.SessionCollection ?? "user_sessions");
        this._domainAccountCollection = database.GetCollection<DomainUserAccount>(settings.Value.DomainUserCollection ?? "domain_accounts");
        this._localUserAccountCollection=database.GetCollection<LocalUserAccount>(settings.Value.LocalUserCollection ?? "local_accounts");
        this._domainCollection=database.GetCollection<AuthDomain>(settings.Value.DomainCollection ?? "auth_domains");
    }

    public async Task<ErrorOr<Success>> CreateAccount(string username, string email, string role, 
        string authDomain, bool isDomainAccount, string? password=default) {
        var domain=await this._domainCollection.Find(d=>d.Name==authDomain).FirstOrDefaultAsync();
        if (domain == null) {
            return Error.NotFound(description:"Auth Domain not found");
        }
        if(!domain.Roles.Contains(role)) {
            return Error.NotFound(description:"Role not found in Auth Domain");
        }
        if (isDomainAccount) {
            var userAccount = await this._domainAccountCollection.Find(u => u.Username == username)
                .FirstOrDefaultAsync();
            if (userAccount is null) {
                //Create Account
                DomainUserAccount account = new DomainUserAccount {
                    _id = ObjectId.GenerateNewId(),
                    Username = username,
                    Email = email,
                    IsDomainAccount = true,
                    AuthDomainRoles = new Dictionary<string, string>() {
                        {authDomain, role},
                    }
                };
                await this._domainAccountCollection.InsertOneAsync(account);
                var createdAccount=await this._domainAccountCollection.Find(e=>e.Username==username)
                    .FirstOrDefaultAsync();
                return createdAccount!=null ? Result.Success : Error.Unexpected(description:"Account not created");
            } else {
                userAccount.AuthDomainRoles[authDomain]=role;
                var filter=Builders<DomainUserAccount>.Filter.Eq(u => u.Username, username);
                var update=Builders<DomainUserAccount>.Update.Set(u => u.AuthDomainRoles, userAccount.AuthDomainRoles);
                var result=await this._domainAccountCollection.UpdateOneAsync(filter, update);
                return result.IsAcknowledged && result.ModifiedCount>0 ? Result.Success : Error.Unexpected(description:"Account not updated");
            }
        } else {
            byte[] key = Bytes.GenerateKey();
            byte[] iv = Bytes.GenerateIV();
            LocalUserAccount account = new LocalUserAccount() {
                _id = ObjectId.GenerateNewId(),
                Username = username,
                Email = email,
                Role = role,
                IsDomainAccount = false,
                AuthDomain = authDomain,
                Key= key,
                IV = iv,
                EncryptedPassword=Strings.Encrypt(password,key, iv)
            };
            await this._localUserAccountCollection.InsertOneAsync(account);
            var createdAccount=await this._localUserAccountCollection.Find(e=>e.Username==username).FirstOrDefaultAsync();
            return createdAccount!=null ? Result.Success : Error.Unexpected(description:"Account not created");
        }
    }
    
    public async Task<bool> LocalUserExists(string username,string authDomain) {
        var userAccount = await this._localUserAccountCollection.Find(u => u.Username == username && u.AuthDomain==authDomain)
            .FirstOrDefaultAsync();
        return userAccount != null;
    }
    
    public async Task<ErrorOr<UserSessionDto>> LoginDomainAccount(string username,string authDomain) {
        var userAccount = await this._domainAccountCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
        if (userAccount == null) {
            return Error.NotFound(description:"User Account not found");
        }
        if(userAccount.AuthDomainRoles.TryGetValue(authDomain, out var role)==false) {
            return Error.Unexpected(description:"User has no role in this domain");
        }
        var userSession = new UserSession {
            _id=ObjectId.GenerateNewId(),
            Username = userAccount.Username,
            Role = role,
            LoginTime = DateTime.Now
        };
        var userSessionDto = new UserSessionDto() {
            Token = userSession._id.ToString(),
            Username = userSession.Username,
            Role = userSession.Role,
        };
        await this._userSessionCollection.InsertOneAsync(userSession);
        return userSessionDto;
    }

    public async Task<ErrorOr<UserSessionDto>> LoginLocalUserAccount(string username,string password,string authDomain) {
        var userAccount = await this._localUserAccountCollection.Find(u => u.Username == username && u.AuthDomain==authDomain)
            .FirstOrDefaultAsync();
        if (userAccount == null) {
            return Error.NotFound(description:"User Account not found");
        }
        var decryptedPassword=Strings.Decrypt(userAccount.EncryptedPassword, userAccount.Key, userAccount.IV);
        if(password!=decryptedPassword) { 
            return Error.NotFound(description:"Incorrect password");;
        }
        var userSession = new UserSession {
            _id=ObjectId.GenerateNewId(),
            Username = userAccount.Username,
            Role = userAccount.Role,
            LoginTime = DateTime.Now
        };
        var userSessionDto = new UserSessionDto() {
            Token = userSession._id.ToString(),
            Username = userSession.Username,
            Role = userSession.Role,
        };
        await this._userSessionCollection.InsertOneAsync(userSession);
        return userSessionDto;
    }

    public async Task<bool> UpdateUserEmail(string username,string email,bool isDomainAccount,string? authDomain=default) {
        if (isDomainAccount) {
            var filter=Builders<DomainUserAccount>.Filter.Eq(u => u.Username, username);
            var update=Builders<DomainUserAccount>.Update.Set(u => u.Email, email);
            var result=await this._domainAccountCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount>0;
        } else {
            var filter=Builders<LocalUserAccount>.Filter.And(
                    Builders<LocalUserAccount>.Filter.Eq(e=>e.Username, username),
                    Builders<LocalUserAccount>.Filter.Eq(e=>e.AuthDomain, authDomain));
            var update=Builders<LocalUserAccount>.Update.Set(u => u.Email, email);
            var result=await this._localUserAccountCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount>0;
        }
    }
    
    public async Task<ErrorOr<Success>> UpdateUserRole(string username,string authDomain,string role,bool isDomainAccount) {
        var domain=await this._domainCollection.Find(d=>d.Name==authDomain).FirstOrDefaultAsync();
        if (domain == null) {
            return Error.NotFound(description:"Auth Domain not found");
        }
        if(!domain.Roles.Contains(role)) {
            return Error.NotFound(description:"Role not found in Auth Domain");
        }
        if (isDomainAccount) {
            var userAccount = await this._domainAccountCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
            if (userAccount == null) {
                return Error.NotFound(description:"UserAccount not found");
            }
            userAccount.AuthDomainRoles[authDomain]=role;
            var filter=Builders<DomainUserAccount>.Filter.Eq(u => u.Username, username);
            var update=Builders<DomainUserAccount>.Update.Set(u => u.AuthDomainRoles, userAccount.AuthDomainRoles);
            var result=await this._domainAccountCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount>0 ? Result.Success : Error.Unexpected(description:"Save Error, Account not updated");
        } else {
            var userAccount=await this._localUserAccountCollection.Find(u => u.Username == username && u.AuthDomain==authDomain)
                .FirstOrDefaultAsync();
            if (userAccount == null) {
                return Error.NotFound(description:"UserAccount not found");
            }
            var filter=Builders<LocalUserAccount>.Filter.And(
                Builders<LocalUserAccount>.Filter.Eq(e=>e.Username, username),
                Builders<LocalUserAccount>.Filter.Eq(e=>e.AuthDomain, authDomain));
            var update=Builders<LocalUserAccount>.Update.Set(u => u.Role, role);
            var result=await this._localUserAccountCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount>0 ? Result.Success : Error.Unexpected(description:"Save Error, Account not updated");
        }
    }
    
    public async Task Logout(ObjectId token) {
        var filter=Builders<UserSession>.Filter.Eq(s => s._id, token);
        var update=Builders<UserSession>.Update.Set(s => s.LogoutTime, DateTime.Now);
        await this._userSessionCollection.UpdateOneAsync(filter, update);
    }
}