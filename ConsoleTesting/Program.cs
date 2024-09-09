// See https://aka.ms/new-console-template for more information

//await CreateSettings();
//await RoleAccountTesting();

using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Domain.Authentication;
using Domain.Settings;
using SETiAuth.Domain.Shared.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;
using Effortless.Net.Encryption;
using SETiAuth.Domain.Shared.Authentication;
using SETiAuth.Domain.Shared.Constants;
using SETiAuth.Domain.Shared.Contracts.Requests;
using SETiAuth.Domain.Shared.Contracts.Responses;


//await TestLogin();
//await TestLogout();
//await CreateLocalUser();
//Console.WriteLine("Press any key to test decryption");
//Console.ReadKey();
//await TestLocalUserDecryption();
//await CreateAuthDomains();
//TestCreateOrgUnit();
//AddUserToGroup();
//RemoveUserFromGroup();
//GetUserRoles();
//RemoveUserFromAll();
//await CreateLdapSettings();

//ExportSetiUsers();
await CreateUsers();

async Task CreateUsers() {
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("auth_db");
	var collection=database.GetCollection<DomainUserAccount>("domain_accounts");
	var lines=File.ReadAllLines(@"C:\Users\aelmendo\Documents\UserData\users.csv");
	foreach (var line in lines) {
		var values=line.Split(',');
		DomainUserAccount account = new DomainUserAccount();
		account._id = values[0].ToLower();
		account.Email = values[2];
		account.FirstName = values[3];
		account.LastName = values[4];
		account.AuthDomainRoles["PurchaseRequestSystem"] = values[5];
		await collection.InsertOneAsync(account);
		Console.WriteLine($"Account inserted: {account._id}");
	}
}

void ExportSetiUsers() {
	using PrincipalContext pc = new PrincipalContext(ContextType.Domain, "172.20.3.5",
		"OU=SETI Users,DC=seti,DC=com",
		"elmendorfal",
		"!23seti");
	UserPrincipal allUsers = new UserPrincipal(pc);
	PrincipalSearcher ps = new PrincipalSearcher(allUsers);
	//List<UserPrincipal> users = [];
	StringBuilder sb = new StringBuilder();
	sb.AppendLine("SamAccount,DisplayName,EmailAddress,UserPrincipalName,GivenName,Surname");
	foreach (var principal in ps.FindAll()) {
		var user = (UserPrincipal)principal;
		sb.AppendLine($"{user.SamAccountName},{user.DisplayName},{user.EmailAddress},{user.UserPrincipalName},{user.GivenName},{user.Surname}");
		Console.WriteLine(
			$"User: {user.SamAccountName} - {user.DisplayName} - {user.EmailAddress} - {user.UserPrincipalName} - {user.GivenName} - {user.Surname}");
		//users.Add(user);
	}
	File.WriteAllText(@"C:\Users\aelmendo\Documents\UserData\users.csv",sb.ToString());
}

async Task CreateAuthDomains() {
	/*
	 * Admin
	 * User
	 * Purchaser
	 * Approver
	 * Guest
	 */
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("auth_db");
	var collection=database.GetCollection<AuthDomain>("auth_domains");
	List<string> roles = ["Admin","Approver","Purchaser","Approver","Guest"];
	AuthDomain authDomain = new AuthDomain { 
		_id = "PurchaseRequestSystem", 
		Roles = roles, 
		Description = "Roles for the Purchase Request System" };
	await collection.InsertOneAsync(authDomain);
	Console.WriteLine("AuthDomain created, check database");
}

async Task CreateLdapSettings() {
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("settings_db");
	var collection=database.GetCollection<LdapSettings>("ldap_settings");
	LdapSettings settings = new LdapSettings() {
		HostIp = "172.20.3.5",
		_id = ObjectId.GenerateNewId(),
		IsLatest = true,
		UserName = "elmendorfal",
		Password = "!23seti",
		DirectoryLdapPath = "LDAP://172.20.3.5:389/OU=Program Access,OU=Groups,OU=SETI,DC=seti,DC=com",
	};
	await collection.InsertOneAsync(settings);
}

async Task TestLocalUserDecryption() {
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("auth_db");
	var collection=database.GetCollection<LocalUserAccount>("user_accounts");
	var account=await collection.Find(e=>e._id=="admin").FirstOrDefaultAsync();
	if (account != null) {
		var decryptedPassword=Strings.Decrypt(account.EncryptedPassword,account.Key, account.IV);
		Console.WriteLine($"Password: {decryptedPassword}");
	} else {
		Console.WriteLine("Error: Failed to find account");
	}
}

async Task CreateLocalUser() {
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("auth_db");
	var collection=database.GetCollection<LocalUserAccount>("user_accounts");
	byte[] key = Bytes.GenerateKey();
	byte[] iv = Bytes.GenerateIV();
	
	LocalUserAccount account = new LocalUserAccount();
	account._id = "admin";
	account.Email = "itsupport@s-et.com";
	account.Role = "Admin";
	account.IsDomainAccount = false;
	account.AuthDomain="PurchaseRequestSystem";
	account.Key= key;
	account.IV = iv;
	account.EncryptedPassword=Strings.Encrypt("password", key, iv);
	await collection.InsertOneAsync(account);
	Console.WriteLine("Account inserted, check database");
}


async Task TestLogin() {
	var client = new HttpClient();
	//client.BaseAddress = new Uri("http://172.20.4.20:5000/api");
	client.BaseAddress = new Uri("http://localhost:5243/api/");
	var response = await client.PostAsJsonAsync($"{HttpClientConstants.LoginEndpoint}",new LoginRequest(){
			Username = "aelmendo", 
			Password = "Drizzle234!",
			IsDomainUser = true 
	}); 
	Console.WriteLine(response.StatusCode);
	var session=await response.Content.ReadFromJsonAsync<LoginResponse>();
	Console.WriteLine(JsonSerializer.Serialize<UserSessionDto>(session?.UserSession, new JsonSerializerOptions() { WriteIndented = true }));
}

async Task TestLogout() {
	var client = new HttpClient();
	//client.BaseAddress = new Uri("http://172.20.4.20:5000/api");
	client.BaseAddress = new Uri("http://localhost:5243/api/");
	var response = await client.PostAsJsonAsync($"{HttpClientConstants.LogoutEndpoint}",new LogoutRequest(){
		Token = "66db0b863c2ed09c7a728764", 
	}); 
	Console.WriteLine(response.StatusCode);
	var session=await response.Content.ReadFromJsonAsync<LoginResponse>();
	Console.WriteLine(JsonSerializer.Serialize<UserSessionDto>(session?.UserSession, new JsonSerializerOptions() { WriteIndented = true }));
}

async Task RoleAccountTesting() {
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("auth_db");
	var collection=database.GetCollection<UserAccount>("user_accounts");
	UserAccount account = new UserAccount() {
		_id = "aelmendorf",
		Email = "aelmendorf@seti.com",
		IsDomainAccount = true
	};
	await collection.InsertOneAsync(account);
	Console.WriteLine("Account inserted, check database");
}

async Task CreateSettings() {
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("settings_db");
	var collection=database.GetCollection<LdapSettings>("login_settings");
	var settings = new LdapSettings() {
		HostIp = "172.20.3.5",
		_id = ObjectId.GenerateNewId(),
		IsLatest = true,
		UserName = "elmendorfal@seti.com",
		Password = "!23seti"
	};
	await collection.InsertOneAsync(settings);
	Console.WriteLine("Settings created, check database");
}

void LoginTest() {
	using (PrincipalContext context = new PrincipalContext(ContextType.Domain, "172.20.3.5", "elmendorfal@seti.com", "!23seti")) {
		UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, "aelmendo");
		if (user != null) {
			Console.WriteLine($"User: {user.SamAccountName} - {user.DisplayName} - {user.EmailAddress} - {user.UserPrincipalName}");
			if (context.ValidateCredentials("aelmendo@seti.com", "Drizzle123!")) {
				Console.WriteLine("User authenticated");
			} else {
				Console.WriteLine("Authentication failed");
			}
		} else {
			Console.WriteLine("User is null");
		}
	}
}