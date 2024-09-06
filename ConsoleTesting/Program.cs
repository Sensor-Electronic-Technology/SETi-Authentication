// See https://aka.ms/new-console-template for more information

//await CreateSettings();
//await RoleAccountTesting();

using System.DirectoryServices.AccountManagement;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Authentication;
using Domain.Settings;
using Domain.Shared.Authentication;
using Domain.Shared.Constants;
using Domain.Shared.Contracts;
using Domain.Shared.Contracts.Requests;
using Domain.Shared.Contracts.Responses;
using MongoDB.Bson;
using MongoDB.Driver;
using Effortless.Net.Encryption;


//await TestLogin();
//await TestLogout();
await CreateLocalUser();
Console.WriteLine("Press any key to test decryption");
Console.ReadKey();
await TestLocalUserDecryption();
//await CreateAuthDomains();
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
	List<string> roles = ["Admin","User","Purchaser","Approver","Guest"];
	AuthDomain authDomain = new AuthDomain();
	authDomain._id=ObjectId.GenerateNewId();
	authDomain.Name = "PurchaseRequestSystem";
	authDomain.Roles = roles;
	authDomain.Description = "Roles for the Purchase Request System";
	await collection.InsertOneAsync(authDomain);
	Console.WriteLine("AuthDomain created, check database");
}

async Task TestLocalUserDecryption() {
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("auth_db");
	var collection=database.GetCollection<LocalUserAccount>("user_accounts");
	var account=await collection.Find(e=>e.Username=="admin").FirstOrDefaultAsync();
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
	account.Username = "admin";
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
		Username = "aelmendorf",
		Email = "aelmendorf@seti.com",
		IsDomainAccount = true
	};
	await collection.InsertOneAsync(account);
	Console.WriteLine("Account inserted, check database");
}

async Task CreateSettings() {
	var client=new MongoClient("mongodb://172.20.3.41:27017");
	var database=client.GetDatabase("settings_db");
	var collection=database.GetCollection<LoginServerSettings>("login_settings");
	var settings = new LoginServerSettings() {
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