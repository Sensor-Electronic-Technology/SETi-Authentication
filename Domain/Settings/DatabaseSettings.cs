namespace Domain.Settings;

public class DatabaseSettings {
    public string? SettingsDatabase { get; init; }
    public string? LoginSettingsCollection { get; init; }
    public string? AuthenticationDatabase { get; init; }
    public string? DomainUserCollection { get; init; }
    public string? LocalUserCollection { get; init; }
    public string? SessionCollection { get; init; }
    public string? DomainCollection { get; init; }
}