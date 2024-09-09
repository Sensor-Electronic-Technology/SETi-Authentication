using Domain.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Services;

public class SettingsService {
    private readonly IMongoCollection<LdapSettings> _settingsCollection;
    public SettingsService(IMongoClient client,IOptions<DatabaseSettings> options) {
        var database = client.GetDatabase(options.Value.SettingsDatabase ?? "settings_db");
        _settingsCollection = database.GetCollection<LdapSettings>(options.Value.LoginSettingsCollection ?? "login_settings");
    }
    
    public async Task<LdapSettings> GetLatestSettings() {
        var settings = await _settingsCollection.Find(s => s.IsLatest).FirstOrDefaultAsync();
        return settings;
    }
    
    public async Task AddLatestSetting(LdapSettings settings) {
       await ClearLatestSetting(); 
       settings.IsLatest = true; 
       settings._id = ObjectId.GenerateNewId();
       await _settingsCollection.InsertOneAsync(settings);
    }
    
    private async Task ClearLatestSetting() {
        var filter= Builders<LdapSettings>.Filter.Eq(s => s.IsLatest, true);
        var update = Builders<LdapSettings>.Update.Set(s => s.IsLatest, false);
        await _settingsCollection.UpdateManyAsync(filter, update);
    }
}