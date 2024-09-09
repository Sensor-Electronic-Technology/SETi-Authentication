using MongoDB.Bson;

namespace Domain.Authentication;

public class UserSession {
    public ObjectId _id { get; set; }
    public DateTime LoginTime { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
}