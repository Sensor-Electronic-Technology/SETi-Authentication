using MongoDB.Bson;

namespace Domain.Settings;

public class LoginServerSettings {
    public ObjectId _id { get; set; }
    public string? HostIp { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool IsLatest { get; set; } = false;
}