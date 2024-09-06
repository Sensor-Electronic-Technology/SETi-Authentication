using MongoDB.Bson;
namespace Domain.Authentication;

public class AuthDomain {
    public ObjectId _id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string> Roles { get; set; } = [];
}