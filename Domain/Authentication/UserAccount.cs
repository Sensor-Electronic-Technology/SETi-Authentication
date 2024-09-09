using MongoDB.Bson;

namespace Domain.Authentication;

public class UserAccount {
    //public ObjectId _id { get; set; }
    public string _id { get; set; }
    public string Email { get; set; }
    public bool IsDomainAccount { get; set; }
}

public class DomainUserAccount : UserAccount {
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Dictionary<string, string> AuthDomainRoles { get; set; } = [];
}

public class LocalUserAccount : UserAccount {
    public string EncryptedPassword { get; set; }
    public byte[]? Key { get; set; }
    public byte[]? IV { get; set; }
    public string? AuthDomain { get; set; }
    public string? Role { get; set; }
    
}

