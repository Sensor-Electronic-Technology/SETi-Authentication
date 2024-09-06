using Domain;
using FastEndpoints;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWindowsService(options => {
    options.ServiceName = "Login-Auth-Api";
});
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddEventLog();
builder.Services.AddSettings(builder);
builder.Services.AddInfrastructure();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFastEndpoints();
var connectionString= builder.Configuration.GetConnectionString("DefaultConnection") ?? "mongodb://172.20.3.41:27017";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));


var app = builder.Build();
var logInService=app.Services.GetService<AuthService>();
if (logInService != null) {
    await logInService.LoadSettings();
} else {
    Console.WriteLine("Error: Failed to load settings");
}
app.UseSwagger();
app.UseSwaggerUI();
app.UseFastEndpoints();
app.Urls.Add("http://172.20.4.20:5000");
app.Run();
